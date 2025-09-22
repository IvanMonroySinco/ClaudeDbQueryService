using ClaudeDbQueryService.Infrastructure.External.Models;

namespace ClaudeDbQueryService.Infrastructure.External.McpServices.ClaudeMcpOrchestrator
{
    public interface IClaudeMcpOrchestrator
    {
        Task<ClaudeResponse> ProcessQueryWithMcpToolsAsync(string query, CancellationToken cancellationToken = default);
    }
}
