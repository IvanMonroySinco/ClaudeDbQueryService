using ClaudeDbQueryService.Infrastructure.External.ApiServices;
using ClaudeDbQueryService.Infrastructure.External.McpServices.McpTools;
using ClaudeDbQueryService.Infrastructure.External.Models;
using Serilog;
using System.Text.Json;

namespace ClaudeDbQueryService.Infrastructure.External.McpServices.ClaudeMcpOrchestrator;

public class ClaudeMcpOrchestrator : IClaudeMcpOrchestrator
{
    private readonly IClaudeApiService _claudeApiService;
    private readonly IMcpToolsService _mcpToolsService;

    public ClaudeMcpOrchestrator(
        IClaudeApiService claudeApiService,
        IMcpToolsService mcpToolsService,
        ILogger<ClaudeMcpOrchestrator> logger)
    {
        _claudeApiService = claudeApiService;
        _mcpToolsService = mcpToolsService;
    }

    public async Task<ClaudeResponse> ProcessQueryWithMcpToolsAsync(string query, CancellationToken cancellationToken = default)
    {
        // Initialize MCP service if needed
        await _mcpToolsService.InitializeAsync(cancellationToken);

        // Get available MCP tools
        var mcpTools = await _mcpToolsService.GetAvailableToolsAsync(cancellationToken);

        // Convert MCP tools to Claude tools format
        var claudeTools = mcpTools.Select(tool => new ClaudeTool
        {
            Name = tool.Name,
            Description = tool.Description,
            InputSchema = tool.InputSchema ?? new { type = "object", properties = new { }, required = new string[0] }
        }).ToList();

        Log.Debug("Processing query with {ToolCount} MCP tools available", claudeTools.Count);

        // Enhanced system prompt for MCP tool usage
        var enhancedSystemPrompt = BuildEnhancedSystemPrompt(claudeTools);

        // Send initial request to Claude with tools
        var claudeResponse = await _claudeApiService.ProcessQueryWithToolsAsync(query, claudeTools, enhancedSystemPrompt, cancellationToken);

        // Process tool calls if Claude wants to use them
        claudeResponse = await ProcessToolCallsAsync(claudeResponse, query, claudeTools, enhancedSystemPrompt, cancellationToken);

        return claudeResponse;
    }

    private async Task<ClaudeResponse> ProcessToolCallsAsync(
        ClaudeResponse response,
        string originalQuery,
        List<ClaudeTool> claudeTools,
        string? systemPrompt,
        CancellationToken cancellationToken)
    {
        const int maxIterations = 3;
        int iteration = 0;

        while (iteration < maxIterations && HasToolUse(response))
        {
            iteration++;
            Log.Debug("Processing tool calls iteration {Iteration}", iteration);

            var toolResults = new List<ClaudeMessage>();

            // Execute all tool calls
            foreach (var content in response.Content.Where(c => c.Type == "tool_use"))
            {
                var toolResult = await ExecuteToolCall(content, cancellationToken);
                toolResults.Add(toolResult);
            }

            // Continue conversation with tool results
            var messages = new List<ClaudeMessage>
            {
                new() { Role = "user", Content = originalQuery }
            };

            // Add Claude's response with tool use (ensuring IDs are present)
            messages.Add(new ClaudeMessage
            {
                Role = "assistant",
                Content = ProcessToolUseContent(response.Content)
            });

            // Add tool results
            messages.AddRange(toolResults);

            var followUpRequest = new ClaudeRequest
            {
                Model = response.Model,
                MaxTokens = 4096,
                Tools = claudeTools,
                ToolChoice = new { type = "auto" },
                Messages = messages,
                System = systemPrompt
            };

            response = await _claudeApiService.SendMessageAsync(followUpRequest, cancellationToken);
        }

        if (iteration >= maxIterations)
        {
            Log.Warning("Reached maximum tool call iterations ({MaxIterations})", maxIterations);
        }

        return response;
    }

    private async Task<ClaudeMessage> ExecuteToolCall(ClaudeContent toolUse, CancellationToken cancellationToken)
    {
        try
        {
            Log.Debug("Executing tool: {ToolName} with input: {Input}", toolUse.Name, JsonSerializer.Serialize(toolUse.Input));

            var result = await _mcpToolsService.ExecuteToolAsync(toolUse.Name!, toolUse.Input!, cancellationToken);

            return new ClaudeMessage
            {
                Role = "user",
                Content = new[]
                {
                    new ClaudeContent
                    {
                        Type = "tool_result",
                        ToolUseId = toolUse.Id!,
                        ContentValue = result.Success ? SerializeResultToString(result.Result) : $"Error: {result.Error}",
                        IsError = !result.Success
                    }
                }
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error executing tool {ToolName}", toolUse.Name);

            return new ClaudeMessage
            {
                Role = "user",
                Content = new[]
                {
                    new ClaudeContent
                    {
                        Type = "tool_result",
                        ToolUseId = toolUse.Id!,
                        ContentValue = $"Tool execution failed: {ex.Message}",
                        IsError = true
                    }
                }
            };
        }
    }

    private static bool HasToolUse(ClaudeResponse response)
    {
        return response.Content.Any(c => c.Type == "tool_use");
    }

    private static object ProcessToolUseContent(IEnumerable<ClaudeContent> content)
    {
        var processedContent = new List<ClaudeContent>();

        foreach (var item in content)
        {
            if (item.Type == "tool_use")
            {
                // Ensure tool_use has a unique ID
                var processedItem = new ClaudeContent
                {
                    Type = item.Type,
                    Name = item.Name,
                    Input = item.Input,
                    Id = item.Id ?? $"toolu_{Guid.NewGuid():N}",
                    Text = item.Text
                };
                processedContent.Add(processedItem);
            }
            else
            {
                processedContent.Add(item);
            }
        }

        return processedContent.ToArray();
    }

    private static string SerializeResultToString(object? result)
    {
        if (result == null)
            return "null";

        if (result is string str)
            return str;

        try
        {
            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }
        catch
        {
            return result.ToString() ?? "Unable to serialize result";
        }
    }

    private static string BuildEnhancedSystemPrompt(List<ClaudeTool> tools)
    {
        var systemPrompt = "La fecha del d�a de hoy es:" + DateTime.UtcNow.ToString() + ". Eres un asistente �til experto con acceso a herramientas especializadas para la gesti�n de maquinaria y equipos.";

        systemPrompt += "\n\nTiene acceso a las siguientes herramientas especializadas para datos de maquinaria y equipos:\n";
        foreach (var tool in tools)
        {
            systemPrompt += $"- {tool.Name}: {tool.Description}\n";
        }

        systemPrompt += "\nUtilice estas herramientas cuando los usuarios pregunten sobre equipos, mantenimiento, �rdenes de trabajo, KPI o informes. Proporcione siempre explicaciones completas y �tiles junto con los datos.";

        return systemPrompt;
    }
}