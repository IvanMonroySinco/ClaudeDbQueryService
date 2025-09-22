using ClaudeDbQueryService.Infrastructure.External.ApiServices;
using ClaudeDbQueryService.Infrastructure.External.Models;
using Serilog;
using SincoSoft.MYE.Common.Models;

namespace ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Queries.GetHealthStatus;

public class GetHealthStatusQuery : IGetHealthStatusQuery
{
    private readonly IClaudeApiService _claudeApiService;

    public GetHealthStatusQuery(
        IClaudeApiService claudeApiService)
    {
        _claudeApiService = claudeApiService;
    }

    public async Task<BaseResponseModel> GetHealthStatus()
    {
        var response = new BaseResponseModel();
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

            Log.Debug("Health status retrieved successfully");
            response.Success = true;
            response.Data = healthResponse;
            response.Message = "Health status retrieved successfully";
            response.StatusCode = 200;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking health status");

            var errorResponse = new QueryHealthResponse
            {
                Status = "unhealthy",
                Version = "1.0.0",
                Server = "ClaudeDbQueryService",
                Timestamp = DateTime.UtcNow
            };

            response.Success = false;
            response.Data = errorResponse;
            response.Message = "Health check failed";
            response.StatusCode = 500;
        }
        return response;
    }
}