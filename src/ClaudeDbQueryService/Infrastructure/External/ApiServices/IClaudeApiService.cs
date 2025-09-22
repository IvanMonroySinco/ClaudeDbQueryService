using ClaudeDbQueryService.Infrastructure.External.Models;

namespace ClaudeDbQueryService.Infrastructure.External.ApiServices;

public interface IClaudeApiService
{
    Task<ClaudeResponse> SendMessageAsync(ClaudeRequest request, CancellationToken cancellationToken = default);
    Task<ClaudeResponse> ProcessQueryAsync(string query, string? systemPrompt = null, CancellationToken cancellationToken = default);
    Task<ClaudeResponse> ProcessQueryWithToolsAsync(string query, List<ClaudeTool> tools, string? systemPrompt = null, CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}