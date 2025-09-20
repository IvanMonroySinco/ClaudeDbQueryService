using ClaudeDbQueryService.Core.Application.Configuration;
using Microsoft.Extensions.Logging;

namespace ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Queries;

public class ValidateClaudeQueryQuery : IValidateClaudeQueryQuery
{
    private readonly ILogger<ValidateClaudeQueryQuery> _logger;

    public ValidateClaudeQueryQuery(ILogger<ValidateClaudeQueryQuery> logger)
    {
        _logger = logger;
    }

    public async Task<ResponseModel> ValidateQuery(string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new ResponseModel
                {
                    IsSuccess = false,
                    Message = "Query cannot be empty",
                    Errors = new List<string> { "Query is required" }
                };
            }

            if (query.Length > 10000)
            {
                return new ResponseModel
                {
                    IsSuccess = false,
                    Message = "Query is too long",
                    Errors = new List<string> { "Query must be less than 10,000 characters" }
                };
            }

            await Task.CompletedTask;

            return new ResponseModel
            {
                IsSuccess = true,
                Data = new { IsValid = true, Query = query },
                Message = "Query is valid"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating query: {Query}", query);

            return new ResponseModel
            {
                IsSuccess = false,
                Message = "Error validating query",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}