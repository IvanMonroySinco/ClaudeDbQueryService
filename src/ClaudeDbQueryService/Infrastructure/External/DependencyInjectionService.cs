using ClaudeDbQueryService.Infrastructure.External.ApiServices;

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

            return services;
        }
    }
}
