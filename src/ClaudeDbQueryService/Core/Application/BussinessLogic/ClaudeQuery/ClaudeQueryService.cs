using ClaudeDbQueryService.Core.Application.Configuration;
using ClaudeDbQueryService.Infrastructure.External.Models;
using ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Commands;
using ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Queries;
using Microsoft.Extensions.Logging;

namespace ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery;

public class ClaudeQueryService : IClaudeQueryService
{
    private readonly IExecuteClaudeQueryCommand _executeCommand;
    private readonly IGetHealthStatusQuery _healthStatusQuery;
    private readonly IValidateClaudeQueryQuery _validateQuery;
    private readonly ILogger<ClaudeQueryService> _logger;

    public ClaudeQueryService(
        IExecuteClaudeQueryCommand executeCommand,
        IGetHealthStatusQuery healthStatusQuery,
        IValidateClaudeQueryQuery validateQuery,
        ILogger<ClaudeQueryService> logger)
    {
        _executeCommand = executeCommand;
        _healthStatusQuery = healthStatusQuery;
        _validateQuery = validateQuery;
        _logger = logger;
    }

    public async Task<ResponseModel> ProcessQuery(QueryQueryRequest request)
    {
        try
        {
            // Validate query first
            var validation = await _validateQuery.ValidateQuery(request.Query);
            if (!validation.IsSuccess)
            {
                return validation;
            }

            // Execute the query
            return await _executeCommand.ExecuteClaudeQuery(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query: {Query}", request.Query);
            return new ResponseModel
            {
                IsSuccess = false,
                Message = "Error processing query",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ResponseModel> GetHealthStatus()
    {
        return await _healthStatusQuery.GetHealthStatus();
    }

}