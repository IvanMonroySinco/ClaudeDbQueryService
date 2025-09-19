using System.Diagnostics;
using System.Text.Json;
using MCPServer.Configuration;
using MCPServer.Models.MCP;
using Microsoft.Extensions.Options;

namespace MCPServer.Services;

public class MCPToolService : IMCPToolService
{
    private readonly IClaudeApiService _claudeApiService;
    private readonly MCPOptions _mcpOptions;
    private readonly ILogger<MCPToolService> _logger;
    private readonly List<MCPTool> _availableTools;

    public MCPToolService(
        IClaudeApiService claudeApiService,
        IOptions<MCPOptions> mcpOptions,
        ILogger<MCPToolService> logger)
    {
        _claudeApiService = claudeApiService;
        _mcpOptions = mcpOptions.Value;
        _logger = logger;
        _availableTools = InitializeTools();
    }

    private List<MCPTool> InitializeTools()
    {
        return new List<MCPTool>
        {
            new()
            {
                Name = "database-tool",
                Description = "Execute database queries and retrieve data from SQL databases",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Natural language query to be converted to SQL" },
                        schema = new { type = "string", description = "Database schema context (optional)" },
                        limit = new { type = "integer", description = "Maximum number of results to return", @default = 100 }
                    },
                    required = new[] { "query" }
                },
                OutputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        sql_query = new { type = "string", description = "Generated SQL query" },
                        results = new { type = "array", description = "Query results" },
                        row_count = new { type = "integer", description = "Number of rows returned" },
                        execution_time_ms = new { type = "number", description = "Query execution time in milliseconds" }
                    }
                },
                Enabled = true
            },
            new()
            {
                Name = "general-assistant",
                Description = "General purpose AI assistant for answering questions and providing information",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        question = new { type = "string", description = "Question or request for the AI assistant" },
                        context = new { type = "string", description = "Additional context for the question (optional)" },
                        format = new { type = "string", description = "Desired response format (text, json, markdown)", @default = "text" }
                    },
                    required = new[] { "question" }
                },
                OutputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        answer = new { type = "string", description = "Assistant's response" },
                        confidence = new { type = "number", description = "Confidence level (0-1)" },
                        sources = new { type = "array", description = "Information sources used (if any)" }
                    }
                },
                Enabled = true
            },
            new()
            {
                Name = "data-analysis",
                Description = "Analyze data patterns, generate insights and create summaries",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        data = new { type = "string", description = "Data to analyze (JSON, CSV, or text format)" },
                        analysis_type = new { type = "string", description = "Type of analysis: summary, trends, patterns, statistics", @default = "summary" },
                        format = new { type = "string", description = "Output format: text, json, table", @default = "text" }
                    },
                    required = new[] { "data" }
                },
                OutputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        insights = new { type = "string", description = "Analysis insights and findings" },
                        summary = new { type = "string", description = "Data summary" },
                        recommendations = new { type = "array", description = "Actionable recommendations" },
                        charts_data = new { type = "object", description = "Data for potential visualizations" }
                    }
                },
                Enabled = true
            }
        };
    }

    public Task<List<MCPTool>> GetAvailableToolsAsync()
    {
        var enabledTools = _availableTools.Where(t => t.Enabled).ToList();
        _logger.LogDebug("Returning {Count} available tools", enabledTools.Count);
        return Task.FromResult(enabledTools);
    }

    public async Task<MCPQueryResponse> ProcessQueryAsync(MCPQueryRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Processing MCP query: {Query}", request.Query);

            // Select tool if not specified
            var toolName = request.Tool ?? await SelectBestToolForQuery(request.Query);
            if (string.IsNullOrEmpty(toolName))
            {
                return new MCPQueryResponse
                {
                    Success = false,
                    Error = "No suitable tool found for this query",
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            // Get the selected tool
            var tool = _availableTools.FirstOrDefault(t => t.Name == toolName && t.Enabled);
            if (tool == null)
            {
                return new MCPQueryResponse
                {
                    Success = false,
                    Error = $"Tool '{toolName}' not found or not enabled",
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            _logger.LogDebug("Using tool: {ToolName} for query processing", toolName);

            // Process the query based on the tool type
            var result = await ProcessWithTool(tool, request, cancellationToken);

            stopwatch.Stop();

            return new MCPQueryResponse
            {
                Success = true,
                Result = result.Result,
                ToolUsed = toolName,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                TokensUsed = result.TokensUsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing MCP query: {Query}", request.Query);

            return new MCPQueryResponse
            {
                Success = false,
                Error = $"Error processing query: {ex.Message}",
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    private async Task<(object Result, MCPTokenUsage TokensUsed)> ProcessWithTool(MCPTool tool, MCPQueryRequest request, CancellationToken cancellationToken)
    {
        var systemPrompt = CreateSystemPromptForTool(tool, request);
        var userPrompt = CreateUserPromptForTool(tool, request);

        var claudeResponse = await _claudeApiService.ProcessQueryAsync(userPrompt, systemPrompt, cancellationToken);

        var result = ExtractResultFromClaudeResponse(claudeResponse, tool);
        var tokensUsed = new MCPTokenUsage
        {
            InputTokens = claudeResponse.Usage?.InputTokens ?? 0,
            OutputTokens = claudeResponse.Usage?.OutputTokens ?? 0,
            TotalTokens = (claudeResponse.Usage?.InputTokens ?? 0) + (claudeResponse.Usage?.OutputTokens ?? 0)
        };

        return (result, tokensUsed);
    }

    private string CreateSystemPromptForTool(MCPTool tool, MCPQueryRequest request)
    {
        return tool.Name switch
        {
            "database-tool" => """
                You are a database query assistant. Your task is to:
                1. Analyze the user's natural language query
                2. Generate appropriate SQL if needed
                3. Provide structured data analysis
                4. Return results in a clear, organized format

                Always respond with JSON containing:
                - sql_query: The SQL query (if applicable)
                - results: Processed results or data
                - row_count: Number of results
                - execution_time_ms: Processing time
                """,

            "general-assistant" => """
                You are a helpful AI assistant. Provide accurate, concise, and helpful responses.
                Always respond with JSON containing:
                - answer: Your response to the question
                - confidence: Your confidence level (0-1)
                - sources: Any relevant sources or references
                """,

            "data-analysis" => """
                You are a data analysis expert. Analyze the provided data and generate insights.
                Always respond with JSON containing:
                - insights: Key findings and patterns
                - summary: Data overview
                - recommendations: Actionable suggestions
                - charts_data: Data formatted for visualization
                """,

            _ => "You are a helpful AI assistant. Respond to the user's query in a clear and structured manner."
        };
    }

    private string CreateUserPromptForTool(MCPTool tool, MCPQueryRequest request)
    {
        var prompt = $"Query: {request.Query}";

        if (request.Parameters != null)
        {
            var parametersJson = JsonSerializer.Serialize(request.Parameters);
            prompt += $"\nParameters: {parametersJson}";
        }

        if (request.Context != null)
        {
            var contextJson = JsonSerializer.Serialize(request.Context);
            prompt += $"\nContext: {contextJson}";
        }

        return prompt;
    }

    private object ExtractResultFromClaudeResponse(Models.Claude.ClaudeResponse claudeResponse, MCPTool tool)
    {
        var content = claudeResponse.Content?.FirstOrDefault()?.Text ?? "";

        try
        {
            // Try to parse as JSON first
            if (content.TrimStart().StartsWith("{"))
            {
                return JsonSerializer.Deserialize<JsonElement>(content);
            }
        }
        catch (JsonException)
        {
            // If JSON parsing fails, return as structured text response
        }

        // Return as a structured response based on tool type
        return tool.Name switch
        {
            "database-tool" => new
            {
                sql_query = "N/A",
                results = content,
                row_count = 0,
                execution_time_ms = 0
            },
            "general-assistant" => new
            {
                answer = content,
                confidence = 0.8,
                sources = new string[0]
            },
            "data-analysis" => new
            {
                insights = content,
                summary = "Analysis completed",
                recommendations = new string[0],
                charts_data = new { }
            },
            _ => new { response = content }
        };
    }

    public async Task<string?> SelectBestToolForQuery(string query)
    {
        try
        {
            var queryLower = query.ToLowerInvariant();

            // Simple keyword-based tool selection
            if (ContainsAny(queryLower, "sql", "database", "table", "select", "query", "data", "records", "db"))
            {
                return "database-tool";
            }

            if (ContainsAny(queryLower, "analyze", "analysis", "pattern", "trend", "insight", "summary", "statistics"))
            {
                return "data-analysis";
            }

            // Default to general assistant
            return "general-assistant";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting best tool for query: {Query}", query);
            return "general-assistant";
        }
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(keyword => text.Contains(keyword));
    }

    public Task<bool> IsToolAvailable(string toolName)
    {
        var isAvailable = _availableTools.Any(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase) && t.Enabled);
        return Task.FromResult(isAvailable);
    }
}