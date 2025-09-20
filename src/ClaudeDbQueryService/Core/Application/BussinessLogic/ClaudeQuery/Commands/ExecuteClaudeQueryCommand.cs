using AutoMapper;
using ClaudeDbQueryService.Core.Application.Configuration;
using ClaudeDbQueryService.Infrastructure.External.Models;
using ClaudeDbQueryService.Infrastructure.External.ApiServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Commands;

public class ExecuteClaudeQueryCommand : IExecuteClaudeQueryCommand
{
    private readonly IMapper _mapper;
    private readonly IClaudeApiService _claudeApiService;
    private readonly ClaudeOptions _claudeOptions;
    private readonly ILogger<ExecuteClaudeQueryCommand> _logger;

    public ExecuteClaudeQueryCommand(
        IMapper mapper,
        IClaudeApiService claudeApiService,
        IOptions<ClaudeOptions> claudeOptions,
        ILogger<ExecuteClaudeQueryCommand> logger)
    {
        _mapper = mapper;
        _claudeApiService = claudeApiService;
        _claudeOptions = claudeOptions.Value;
        _logger = logger;
    }

    public async Task<ResponseModel> ExecuteClaudeQuery(QueryQueryRequest request)
    {
        try
        {
            _logger.LogInformation("Executing Claude query: {Query} for user: {UserId}",
                request.Query, request.UserId ?? "anonymous");

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

            var claudeResponse = await _claudeApiService.SendMessageAsync(claudeRequest);
            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            var response = new QueryQueryResponse
            {
                Success = true,
                Result = claudeResponse.Content?.FirstOrDefault()?.Text,
                ToolUsed = "claude-api",
                ExecutionTimeMs = (long)executionTime,
                TokensUsed = new QueryTokenUsage
                {
                    InputTokens = claudeResponse.Usage?.InputTokens ?? 0,
                    OutputTokens = claudeResponse.Usage?.OutputTokens ?? 0,
                    TotalTokens = (claudeResponse.Usage?.InputTokens ?? 0) + (claudeResponse.Usage?.OutputTokens ?? 0)
                },
                Timestamp = DateTime.UtcNow
            };

            return new ResponseModel
            {
                IsSuccess = true,
                Data = response,
                Message = "Query executed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Claude query: {Query}", request.Query);

            var errorResponse = new QueryQueryResponse
            {
                Success = false,
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            };

            return new ResponseModel
            {
                IsSuccess = false,
                Data = errorResponse,
                Message = "Error executing query",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}