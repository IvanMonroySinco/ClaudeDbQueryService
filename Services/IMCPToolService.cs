using MCPServer.Models.MCP;

namespace MCPServer.Services;

public interface IMCPToolService
{
    Task<List<MCPTool>> GetAvailableToolsAsync();
    Task<MCPQueryResponse> ProcessQueryAsync(MCPQueryRequest request, CancellationToken cancellationToken = default);
    Task<string?> SelectBestToolForQuery(string query);
    Task<bool> IsToolAvailable(string toolName);
}