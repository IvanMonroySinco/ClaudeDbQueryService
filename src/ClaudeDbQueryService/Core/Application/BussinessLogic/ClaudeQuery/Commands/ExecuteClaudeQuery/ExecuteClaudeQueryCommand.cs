using ClaudeDbQueryService.Core.Application.Configuration;
using ClaudeDbQueryService.Infrastructure.External.ApiServices;
using ClaudeDbQueryService.Infrastructure.External.Models;
using ClaudeDbQueryService.Infrastructure.External.McpServices;
using Microsoft.Extensions.Options;
using Serilog;
using SincoSoft.MYE.Common.Models;

namespace ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Commands.ExecuteClaudeQuery;

public class ExecuteClaudeQueryCommand : IExecuteClaudeQueryCommand
{
    private readonly IClaudeApiService _claudeApiService;
    private readonly IClaudeMcpOrchestrator _claudeMcpOrchestrator;
    private readonly ClaudeOptions _claudeOptions;

    public ExecuteClaudeQueryCommand(
        IClaudeApiService claudeApiService,
        IClaudeMcpOrchestrator claudeMcpOrchestrator,
        IOptions<ClaudeOptions> claudeOptions)
    {
        _claudeApiService = claudeApiService;
        _claudeMcpOrchestrator = claudeMcpOrchestrator;
        _claudeOptions = claudeOptions.Value;
    }

    public async Task<BaseResponseModel> ExecuteClaudeQuery(QueryQueryRequest request)
    {
        var response = new BaseResponseModel();
        try
        {
            Log.Debug("Executing Claude query with MCP tools: {Query}", request.Query);
            var startTime = DateTime.UtcNow;

            // Use Claude + MCP orchestrator for enhanced capabilities
            var systemPrompt = "You are an expert assistant for machinery and equipment management. Use the available tools to provide accurate, data-driven responses about equipment status, maintenance, work orders, and operational analytics.";

            var claudeData = await _claudeMcpOrchestrator.ProcessQueryWithMcpToolsAsync(request.Query, systemPrompt);
            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // Extract final response text from Claude's response
            var responseText = ExtractResponseText(claudeData);
            var toolsUsed = ExtractToolsUsed(claudeData);

            var claudeResponse = new QueryQueryResponse
            {
                Success = true,
                Result = responseText,
                ToolUsed = string.Join(", ", toolsUsed),
                ExecutionTimeMs = (long)executionTime,
                TokensUsed = new QueryTokenUsage
                {
                    InputTokens = claudeData.Usage?.InputTokens ?? 0,
                    OutputTokens = claudeData.Usage?.OutputTokens ?? 0,
                    TotalTokens = (claudeData.Usage?.InputTokens ?? 0) + (claudeData.Usage?.OutputTokens ?? 0)
                },
                Timestamp = DateTime.UtcNow
            };

            Log.Error("Query executed successfully");
            response.Data = claudeResponse;
            response.StatusCode = 200;
            response.Message = "�Consulta realizada correctamente!";
            return response;

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error executing Claude query: {Query}, message: {Message}", request.Query, ex.Message);

            response.Success = false;
            response.StatusCode = 400;
            response.Message = "Se ha producido un error al procesar la solicitud. Por favor, int�ntelo nuevamente m�s tarde.";
            return response;
        }
    }

    private static string ExtractResponseText(ClaudeResponse claudeResponse)
    {
        // Get the final text response, ignoring tool_use content
        var textContent = claudeResponse.Content
            .Where(c => c.Type == "text" && !string.IsNullOrEmpty(c.Text))
            .Select(c => c.Text)
            .ToList();

        return string.Join("\n", textContent);
    }

    private static List<string> ExtractToolsUsed(ClaudeResponse claudeResponse)
    {
        // Extract names of tools that were used
        var toolsUsed = claudeResponse.Content
            .Where(c => c.Type == "tool_use" && !string.IsNullOrEmpty(c.Name))
            .Select(c => c.Name!)
            .Distinct()
            .ToList();

        return toolsUsed.Any() ? toolsUsed : new List<string> { "claude-api" };
    }
}