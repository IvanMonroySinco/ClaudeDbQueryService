namespace ClaudeDbQueryService.Core.Application.Configuration;

public class QueryServiceOptions
{
    public const string SectionName = "QueryService";

    public string ServerName { get; set; } = "ClaudeDbQueryService";
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = "Claude Database Query Service - Enterprise microservice for AI-powered query processing";
    public string HealthCheckPath { get; set; } = "/api/ClaudeQuery/health";
    public string QueryPath { get; set; } = "/api/ClaudeQuery/query";
    public bool EnableDetailedLogging { get; set; } = true;
    public int DefaultTimeoutSeconds { get; set; } = 60;
}

