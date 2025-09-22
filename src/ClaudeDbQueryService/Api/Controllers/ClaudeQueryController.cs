using ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Commands.AskClaude;
using ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Queries.GetHealthStatus;
using ClaudeDbQueryService.Core.Application.Configuration;
using ClaudeDbQueryService.Infrastructure.External.ApiServices;
using ClaudeDbQueryService.Infrastructure.External.McpServices.McpTools;
using ClaudeDbQueryService.Infrastructure.External.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Serilog;
using SincoSoft.MYE.Common;

namespace ClaudeDbQueryService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClaudeQueryController : ControllerBase
{
    private readonly IAskClaudeCommand _claudeQueryService;
    private readonly IClaudeApiService _claudeApiService;

    public ClaudeQueryController(
        IAskClaudeCommand claudeQueryService,
        IClaudeApiService claudeApiService,
        ILogger<ClaudeQueryController> logger)
    {
        _claudeQueryService = claudeQueryService;
        _claudeApiService = claudeApiService;
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


    [HttpGet("mcp/health")]
    public async Task<IActionResult> GetMcpHealth([FromServices] IMcpToolsService mcpService)
    {
        try
        {
            var isHealthy = await mcpService.IsHealthyAsync();
            var status = new
            {
                mcp_status = isHealthy ? "healthy" : "unhealthy",
                timestamp = DateTime.UtcNow,
                message = isHealthy ? "MCP tools service is operational" : "MCP tools service is not responding"
            };

            return isHealthy ? Ok(status) : StatusCode(503, status);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking MCP health");
            return StatusCode(503, new { mcp_status = "error", message = ex.Message, timestamp = DateTime.UtcNow });
        }
    }

    [HttpGet("mcp/tools")]
    public async Task<IActionResult> GetMcpTools([FromServices] IMcpToolsService mcpService)
    {
        try
        {
            await mcpService.InitializeAsync();
            var tools = await mcpService.GetAvailableToolsAsync();

            var toolsInfo = new
            {
                tools_count = tools.Count(),
                tools = tools.Select(t => new
                {
                    name = t.Name,
                    description = t.Description
                }),
                timestamp = DateTime.UtcNow
            };

            return this.HandleResponse(toolsInfo);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting MCP tools");
            return StatusCode(500, new { error = ex.Message, timestamp = DateTime.UtcNow });
        }
    }
}