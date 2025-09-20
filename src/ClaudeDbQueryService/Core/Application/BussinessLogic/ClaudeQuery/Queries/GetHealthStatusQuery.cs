using ClaudeDbQueryService.Core.Application.Configuration;
using ClaudeDbQueryService.Infrastructure.External.Models;
using ClaudeDbQueryService.Infrastructure.External.ApiServices;
using Microsoft.Extensions.Logging;

namespace ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Queries;

public class GetHealthStatusQuery : IGetHealthStatusQuery
{
    private readonly IClaudeApiService _claudeApiService;
    private readonly ILogger<GetHealthStatusQuery> _logger;

    public GetHealthStatusQuery(
        IClaudeApiService claudeApiService,
        ILogger<GetHealthStatusQuery> logger)
    {
        _claudeApiService = claudeApiService;
        _logger = logger;
    }

    public async Task<ResponseModel> GetHealthStatus()
    {
        try
        {
            var claudeHealthy = await _claudeApiService.IsHealthyAsync();

            var healthResponse = new QueryHealthResponse
            {
                Status = claudeHealthy ? "healthy" : "degraded",
                Version = "1.0.0",
                Server = "ClaudeDbQueryService",
                Timestamp = DateTime.UtcNow
            };

            return new ResponseModel
            {
                IsSuccess = true,
                Data = healthResponse,
                Message = "Health status retrieved successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health status");

            var errorResponse = new QueryHealthResponse
            {
                Status = "unhealthy",
                Version = "1.0.0",
                Server = "ClaudeDbQueryService",
                Timestamp = DateTime.UtcNow
            };

            return new ResponseModel
            {
                IsSuccess = false,
                Data = errorResponse,
                Message = "Health check failed",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}