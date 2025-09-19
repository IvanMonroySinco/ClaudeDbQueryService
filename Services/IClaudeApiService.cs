using MCPServer.Models.Claude;

namespace MCPServer.Services;

public interface IClaudeApiService
{
    Task<ClaudeResponse> SendMessageAsync(ClaudeRequest request, CancellationToken cancellationToken = default);
    Task<ClaudeResponse> ProcessQueryAsync(string query, string? systemPrompt = null, CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}