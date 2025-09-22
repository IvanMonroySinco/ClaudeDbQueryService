using ClaudeDbQueryService.Core.Application.Configuration;
using ClaudeDbQueryService.Infrastructure.External.ApiServices;
using ClaudeDbQueryService.Infrastructure.External.Models;
using Microsoft.Extensions.Options;
using Serilog;
using SincoSoft.MYE.Common.Models;

namespace ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Commands.ExecuteClaudeQuery;

public class ExecuteClaudeQueryCommand : IExecuteClaudeQueryCommand
{
    private readonly IClaudeApiService _claudeApiService;
    private readonly ClaudeOptions _claudeOptions;

    public ExecuteClaudeQueryCommand(
        IClaudeApiService claudeApiService,
        IOptions<ClaudeOptions> claudeOptions)
    {
        _claudeApiService = claudeApiService;
        _claudeOptions = claudeOptions.Value;
    }

    public async Task<BaseResponseModel> ExecuteClaudeQuery(QueryQueryRequest request)
    {
        var response = new BaseResponseModel();
        try
        {
            Log.Debug("Executing Claude query: {Query} for user: {UserId}", request.Query);
            var startTime = DateTime.UtcNow;

            // Create Claude API request using configuration
            var claudeRequest = new ClaudeRequest
            {
                Model = _claudeOptions.Model,
                MaxTokens = _claudeOptions.MaxTokens,
                Temperature = _claudeOptions.Temperature,
                TopP = _claudeOptions.TopP,
                Messages = new List<ClaudeMessage>
                {
                    new() { Role = "user", Content = request.Query }
                }
            };

            var claudeData = await _claudeApiService.SendMessageAsync(claudeRequest);
            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            var claudeResponse = new QueryQueryResponse
            {
                Success = true,
                Result = claudeData.Content?.FirstOrDefault()?.Text,
                ToolUsed = "claude-api",
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
            response.Message = "¡Consulta realizada correctamente!";
            return response;

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error executing Claude query: {Query}, message: {Message}", request.Query, ex.Message);

            response.Success = false;
            response.StatusCode = 400;
            response.Message = "Se ha producido un error al procesar la solicitud. Por favor, inténtelo nuevamente más tarde.";
            return response;
        }
    }
}