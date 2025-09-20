using Microsoft.AspNetCore.Mvc;
using ClaudeDbQueryService.Infrastructure.External.Models;
using ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery;
using ClaudeDbQueryService.Infrastructure.External.ApiServices;
using ClaudeDbQueryService.Core.Application.Configuration;
using Microsoft.Extensions.Options;

namespace ClaudeDbQueryService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClaudeQueryController : ControllerBase
{
    private readonly IClaudeQueryService _claudeQueryService;
    private readonly IClaudeApiService _claudeApiService;
    private readonly QueryServiceOptions _queryOptions;
    private readonly ILogger<ClaudeQueryController> _logger;

    public ClaudeQueryController(
        IClaudeQueryService claudeQueryService,
        IClaudeApiService claudeApiService,
        IOptions<QueryServiceOptions> queryOptions,
        ILogger<ClaudeQueryController> logger)
    {
        _claudeQueryService = claudeQueryService;
        _claudeApiService = claudeApiService;
        _queryOptions = queryOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint for Claude Query service
    /// </summary>
    /// <returns>Server health status</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(QueryHealthResponse), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetHealth([FromServices] IClaudeQueryService service)
    {
        try
        {
            _logger.LogDebug("Health check requested");

            var data = await service.GetHealthStatus();
            return data.IsSuccess ? Ok(data.Data) : StatusCode(500, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");

            var errorResponse = new QueryHealthResponse
            {
                Status = "unhealthy",
                Version = _queryOptions.Version,
                Server = _queryOptions.ServerName,
                Timestamp = DateTime.UtcNow
            };

            return StatusCode(500, errorResponse);
        }
    }

  

    /// <summary>
    /// Process a query using Claude AI
    /// </summary>
    /// <param name="request">Query request</param>
    /// <returns>Query processing result</returns>
    [HttpPost("query")]
    [ProducesResponseType(typeof(QueryQueryResponse), 200)]
    [ProducesResponseType(typeof(QueryErrorResponse), 400)]
    [ProducesResponseType(typeof(QueryErrorResponse), 500)]
    public async Task<IActionResult> ProcessQuery([FromBody] QueryQueryRequest request, [FromServices] IClaudeQueryService service)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                var validationError = new QueryErrorResponse
                {
                    Error = "Query cannot be empty",
                    ErrorCode = "VALIDATION_ERROR"
                };
                return BadRequest(validationError);
            }

            _logger.LogInformation("Processing query: {Query} for user: {UserId}",
                request.Query, request.UserId ?? "anonymous");

            var data = await service.ProcessQuery(request);
            return data.IsSuccess ? Ok(data.Data) : BadRequest(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query: {Query}", request.Query);

            var errorResponse = new QueryErrorResponse
            {
                Error = $"Internal server error: {ex.Message}",
                ErrorCode = "INTERNAL_ERROR"
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
            server_name = _queryOptions.ServerName,
            version = _queryOptions.Version,
            description = _queryOptions.Description,
            protocol_version = "2025-01-01",
            capabilities = new
            {
                tools = true,
                health_check = true,
                query_processing = true,
                claude_integration = true
            },
            endpoints = new
            {
                health = _queryOptions.HealthCheckPath,
                tools = _queryOptions.ToolsPath,
                query = _queryOptions.QueryPath,
                info = "/api/ClaudeQuery/info"
            },
            timestamp = DateTime.UtcNow
        };

        return Ok(serverInfo);
    }
}