using System.Diagnostics;
using ClaudeDbQueryService.Core.Application.Configuration;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;
using Serilog;

namespace ClaudeDbQueryService.Infrastructure.External.McpServices.McpTools;

public class McpToolsService : IMcpToolsService
{
    private readonly McpOptions _options;
    private IMcpClient? _mcpClient;
    private readonly object _lockObject = new();
    private bool _disposed = false;
    private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
    private bool _isInitialized = false;

    public McpToolsService(IOptions<McpOptions> options, ILogger<McpToolsService> logger)
    {
        _options = options.Value;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
            return;

        await _initializationSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
                return;

            Log.Information("Initializing MCP Tools Service with executable: {ExecutablePath}", _options.ExecutablePath);

            if (!File.Exists(_options.ExecutablePath))
            {
                throw new FileNotFoundException($"MCP executable not found at: {_options.ExecutablePath}");
            }

            // Create MCP client with stdio transport (handles process management internally)
            var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Command = _options.ExecutablePath,
                Name = "ClaudeDbQueryService"
            });

            var options = new McpClientOptions();
            _mcpClient = await McpClientFactory.CreateAsync(clientTransport, options, cancellationToken: cancellationToken);

            Log.Information("MCP Client connected successfully");
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize MCP Tools Service");
            await CleanupAsync();
            throw;
        }
        finally
        {
            _initializationSemaphore.Release();
        }
    }

    public async Task<IEnumerable<McpTool>> GetAvailableToolsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        try
        {
            var tools = await _mcpClient!.ListToolsAsync();

            return tools.Select(tool => new McpTool
            {
                Name = tool.Name,
                Description = tool.Description ?? string.Empty,
                InputSchema = new { type = "object", properties = new { }, required = new string[0] }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get available MCP tools");
            throw;
        }
    }

    public async Task<McpToolResult> ExecuteToolAsync(string toolName, object parameters, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var argumentsDict = parameters as IReadOnlyDictionary<string, object?> ??
                               new Dictionary<string, object?> { ["input"] = parameters };

            var result = await _mcpClient!.CallToolAsync(toolName, argumentsDict, cancellationToken: cancellationToken);

            stopwatch.Stop();

            Log.Debug("MCP tool {ToolName} executed successfully in {ElapsedMs}ms", toolName, stopwatch.ElapsedMilliseconds);

            return new McpToolResult
            {
                Success = true,
                Result = result,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Log.Error(ex, "Failed to execute MCP tool: {ToolName}", toolName);

            return new McpToolResult
            {
                Success = false,
                Error = ex.Message,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isInitialized)
            {
                await InitializeAsync(cancellationToken);
            }

            // Check if MCP client is still connected
            if (_mcpClient == null)
            {
                Log.Warning("MCP client is not initialized");
                return false;
            }

            // Try to get tools as a health check
            var tools = await GetAvailableToolsAsync(cancellationToken);
            return tools.Any();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "MCP health check failed");
            return false;
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (!_isInitialized)
        {
            await InitializeAsync(cancellationToken);
        }

        if (_mcpClient == null)
        {
            throw new InvalidOperationException("MCP client is not initialized");
        }
    }

    private async Task CleanupAsync()
    {
        try
        {
            if (_mcpClient != null)
            {
                await _mcpClient.DisposeAsync();
                _mcpClient = null;
            }

            _isInitialized = false;
        }
        catch (Exception ex)
        {
           Log.Error(ex, "Error during MCP cleanup");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        CleanupAsync().GetAwaiter().GetResult();
        _initializationSemaphore?.Dispose();
        _disposed = true;
    }
}