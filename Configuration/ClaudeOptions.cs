namespace MCPServer.Configuration;

public class ClaudeOptions
{
    public const string SectionName = "Claude";

    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 4096;
    public int TimeoutSeconds { get; set; } = 60;
    public double Temperature { get; set; } = 0.7;
    public double TopP { get; set; } = 0.9;
}

public class MCPOptions
{
    public const string SectionName = "MCP";

    public string ServerName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool EnableDetailedLogging { get; set; } = true;
    public string HealthCheckPath { get; set; } = "/health";
    public string ToolsPath { get; set; } = "/tools";
    public string QueryPath { get; set; } = "/query";
}