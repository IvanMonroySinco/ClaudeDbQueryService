namespace ClaudeDbQueryService.Core.Application.Configuration;

public class McpOptions
{
    public const string SectionName = "Mcp";

    public string ExecutablePath { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableLogging { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
}