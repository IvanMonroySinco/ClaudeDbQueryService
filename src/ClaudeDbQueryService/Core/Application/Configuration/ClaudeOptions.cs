namespace ClaudeDbQueryService.Core.Application.Configuration;

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

    // Retry configuration
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 2;
    public double RetryMultiplier { get; set; } = 2.0;
}