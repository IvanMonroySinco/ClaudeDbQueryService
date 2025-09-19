using Microsoft.AspNetCore.Mvc;
using MCPServer.Models.MCP;
using MCPServer.Services;
using MCPServer.Configuration;
using Microsoft.Extensions.Options;

namespace MCPServer.Controllers;

[ApiController]
[Route("")]
public class MCPController : ControllerBase
{
    private readonly IMCPToolService _mcpToolService;
    private readonly IClaudeApiService _claudeApiService;
    private readonly MCPOptions _mcpOptions;
    private readonly ILogger<MCPController> _logger;

    public MCPController(
        IMCPToolService mcpToolService,
        IClaudeApiService claudeApiService,
        IOptions<MCPOptions> mcpOptions,
        ILogger<MCPController> logger)
    {
        _mcpToolService = mcpToolService;
        _claudeApiService = claudeApiService;
        _mcpOptions = mcpOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint for MCP server
    /// </summary>
    /// <returns>Server health status</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(MCPHealthResponse), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            _logger.LogDebug("Health check requested");

            var claudeHealthy = await _claudeApiService.IsHealthyAsync();

            var healthResponse = new MCPHealthResponse
            {
                Status = claudeHealthy ? "healthy" : "degraded",
                Version = _mcpOptions.Version,
                Server = _mcpOptions.ServerName,
                Timestamp = DateTime.UtcNow
            };

            var statusCode = claudeHealthy ? 200 : 503;
            return StatusCode(statusCode, healthResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");

            var errorResponse = new MCPHealthResponse
            {
                Status = "unhealthy",
                Version = _mcpOptions.Version,
                Server = _mcpOptions.ServerName,
                Timestamp = DateTime.UtcNow
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Get available MCP tools
    /// </summary>
    /// <returns>List of available tools</returns>
    [HttpGet("tools")]
    [ProducesResponseType(typeof(MCPToolsResponse), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetTools()
    {
        try
        {
            _logger.LogDebug("Tools list requested");

            var tools = await _mcpToolService.GetAvailableToolsAsync();

            var response = new MCPToolsResponse
            {
                Tools = tools,
                Server = _mcpOptions.ServerName,
                Version = _mcpOptions.Version
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tools");

            var errorResponse = new MCPErrorResponse
            {
                Error = "Error retrieving available tools",
                ErrorCode = "TOOLS_ERROR"
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Process a query using MCP tools and Claude AI
    /// </summary>
    /// <param name="request">Query request</param>
    /// <returns>Query processing result</returns>
    [HttpPost("query")]
    [ProducesResponseType(typeof(MCPQueryResponse), 200)]
    [ProducesResponseType(typeof(MCPErrorResponse), 400)]
    [ProducesResponseType(typeof(MCPErrorResponse), 500)]
    public async Task<IActionResult> ProcessQuery([FromBody] MCPQueryRequest request)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                var validationError = new MCPErrorResponse
                {
                    Error = "Query cannot be empty",
                    ErrorCode = "VALIDATION_ERROR"
                };
                return BadRequest(validationError);
            }

            _logger.LogInformation("Processing query: {Query} for user: {UserId}",
                request.Query, request.UserId ?? "anonymous");

            // Set up cancellation token with timeout
            var timeout = request.Timeout ?? 60;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));

            var response = await _mcpToolService.ProcessQueryAsync(request, cts.Token);

            if (response.Success)
            {
                _logger.LogInformation("Query processed successfully in {ExecutionTime}ms using tool: {Tool}",
                    response.ExecutionTimeMs, response.ToolUsed);
            }
            else
            {
                _logger.LogWarning("Query processing failed: {Error}", response.Error);
            }

            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Query processing timed out: {Query}", request.Query);

            var timeoutResponse = new MCPQueryResponse
            {
                Success = false,
                Error = "Query processing timed out",
                ExecutionTimeMs = request.Timeout * 1000 ?? 60000
            };

            return Ok(timeoutResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query: {Query}", request.Query);

            var errorResponse = new MCPErrorResponse
            {
                Error = $"Internal server error: {ex.Message}",
                ErrorCode = "INTERNAL_ERROR"
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Check if a specific tool is available
    /// </summary>
    /// <param name="toolName">Name of the tool to check</param>
    /// <returns>Tool availability status</returns>
    [HttpGet("tools/{toolName}/status")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(MCPErrorResponse), 404)]
    public async Task<IActionResult> GetToolStatus(string toolName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(toolName))
            {
                var validationError = new MCPErrorResponse
                {
                    Error = "Tool name cannot be empty",
                    ErrorCode = "VALIDATION_ERROR"
                };
                return BadRequest(validationError);
            }

            var isAvailable = await _mcpToolService.IsToolAvailable(toolName);

            if (!isAvailable)
            {
                var notFoundError = new MCPErrorResponse
                {
                    Error = $"Tool '{toolName}' not found or not available",
                    ErrorCode = "TOOL_NOT_FOUND"
                };
                return NotFound(notFoundError);
            }

            var statusResponse = new
            {
                tool_name = toolName,
                status = "available",
                timestamp = DateTime.UtcNow
            };

            return Ok(statusResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking tool status: {ToolName}", toolName);

            var errorResponse = new MCPErrorResponse
            {
                Error = "Error checking tool status",
                ErrorCode = "TOOL_STATUS_ERROR"
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Get server information and capabilities
    /// </summary>
    /// <returns>Server information</returns>
    [HttpGet("info")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult GetServerInfo()
    {
        var serverInfo = new
        {
            server_name = _mcpOptions.ServerName,
            version = _mcpOptions.Version,
            description = _mcpOptions.Description,
            protocol_version = "2025-06-18",
            capabilities = new
            {
                tools = true,
                health_check = true,
                query_processing = true,
                claude_integration = true
            },
            endpoints = new
            {
                health = _mcpOptions.HealthCheckPath,
                tools = _mcpOptions.ToolsPath,
                query = _mcpOptions.QueryPath,
                info = "/info"
            },
            timestamp = DateTime.UtcNow
        };

        return Ok(serverInfo);
    }

    /// <summary>
    /// Test endpoint to verify Claude API connectivity with different models
    /// </summary>
    /// <returns>Test results for available models</returns>
    [HttpGet("test-models")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> TestModels()
    {
        var models = new[]
        {
            "claude-3-haiku-20240307",
            "claude-3-sonnet-20240229",
            "claude-3-opus-20240229",
            "claude-3-5-sonnet-20240620",
            "claude-3-5-sonnet-20241022"
        };

        var results = new List<object>();

        foreach (var model in models)
        {
            try
            {
                _logger.LogDebug("Testing model: {Model}", model);

                var testRequest = new Models.Claude.ClaudeRequest
                {
                    Model = model,
                    MaxTokens = 5,
                    Messages = new List<Models.Claude.ClaudeMessage>
                    {
                        new() { Role = "user", Content = "Hi" }
                    }
                };

                var response = await _claudeApiService.SendMessageAsync(testRequest);
                results.Add(new
                {
                    model = model,
                    status = "available",
                    response_length = response.Content?.FirstOrDefault()?.Text?.Length ?? 0
                });
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    model = model,
                    status = "error",
                    error = ex.Message
                });
            }
        }

        return Ok(new
        {
            tested_at = DateTime.UtcNow,
            results = results
        });
    }
}