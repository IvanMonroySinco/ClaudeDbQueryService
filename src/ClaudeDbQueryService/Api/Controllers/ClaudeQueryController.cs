using Microsoft.AspNetCore.Mvc;
using ClaudeDbQueryService.Infrastructure.External.Models;
using ClaudeDbQueryService.Infrastructure.External.ApiServices;
using ClaudeDbQueryService.Core.Application.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using SincoSoft.MYE.Common;
using ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Queries.GetHealthStatus;
using ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Commands.AskClaude;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ClaudeDbQueryService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClaudeQueryController : ControllerBase
{
    private readonly IAskClaudeCommand _claudeQueryService;
    private readonly IClaudeApiService _claudeApiService;
    private readonly QueryServiceOptions _queryOptions;
    private readonly ILogger<ClaudeQueryController> _logger;

    public ClaudeQueryController(
        IAskClaudeCommand claudeQueryService,
        IClaudeApiService claudeApiService,
        IOptions<QueryServiceOptions> queryOptions,
        ILogger<ClaudeQueryController> logger)
    {
        _claudeQueryService = claudeQueryService;
        _claudeApiService = claudeApiService;
        _queryOptions = queryOptions.Value;
        _logger = logger;
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth([FromServices] IGetHealthStatusQuery service)
    {
        Log.Debug("Starting Health check requested");

        var data = await service.GetHealthStatus();
        return this.HandleResponse(data);
    }


    [HttpPost("query")]
    public async Task<IActionResult> ProcessQuery([FromBody] QueryQueryRequest request, [FromServices] IAskClaudeCommand service)
    {
        Log.Debug("Starting MaintenanceByNoEquipmentState");
        Log.Debug("Processing query: {Query} for user: {UserId}", request.Query);

        var data = await service.ProcessQuery(request);
        return this.HandleResponse(data);
    }

    [HttpGet("info")]
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
                query = _queryOptions.QueryPath,
                info = "/api/ClaudeQuery/info"
            },
            timestamp = DateTime.UtcNow
        };

        return this.HandleResponse(serverInfo);
    }
}