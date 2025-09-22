using ClaudeDbQueryService.Infrastructure.External.ApiServices;
using ClaudeDbQueryService.Core.Application.Configuration;
using ClaudeDbQueryService.Infrastructure.External.McpServices.ClaudeMcpOrchestrator;
using ClaudeDbQueryService.Infrastructure.External.McpServices.McpTools;

namespace ClaudeDbQueryService.Infrastructure.External
{
    public static class DependencyInjectionService
    {
        public static IServiceCollection AddExternal(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure HttpClient for Claude API
            services.AddHttpClient<IClaudeApiService, ClaudeApiService>();

            // Register infrastructure services
            services.AddScoped<IClaudeApiService, ClaudeApiService>();

            // Register MCP services
            services.AddSingleton<IMcpToolsService, McpToolsService>();
            services.AddScoped<IClaudeMcpOrchestrator, ClaudeMcpOrchestrator>();

            return services;
        }
    }
}
