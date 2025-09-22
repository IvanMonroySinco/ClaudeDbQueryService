using ClaudeDbQueryService.Infrastructure.External.Models;

namespace ClaudeDbQueryService.Infrastructure.External.McpServices;

public interface IMcpToolsService : IDisposable
{
    Task<IEnumerable<McpTool>> GetAvailableToolsAsync(CancellationToken cancellationToken = default);
    Task<McpToolResult> ExecuteToolAsync(string toolName, object parameters, CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public class McpTool
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object? InputSchema { get; set; }
}

public class McpToolResult
{
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
    public long ExecutionTimeMs { get; set; }
}