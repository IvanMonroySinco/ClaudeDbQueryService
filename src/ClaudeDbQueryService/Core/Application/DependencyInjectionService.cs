using AutoMapper;
using ClaudeDbQueryService.Core.Application.Configuration;
using ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Queries.GetHealthStatus;
using ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Commands.ExecuteClaudeQuery;
using ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Commands.AskClaude;

namespace ClaudeDbQueryService.Core.Application;

public static class DependencyInjectionService
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new MapperProfile());
        });

        services.AddSingleton(mapper.CreateMapper());

        // Register Commands
        services.AddTransient<IExecuteClaudeQueryCommand, ExecuteClaudeQueryCommand>();

        // Register Queries
        services.AddTransient<IGetHealthStatusQuery, GetHealthStatusQuery>();

        // Register main service
        services.AddTransient<IAskClaudeCommand, AskClaudeCommand>();


        return services;
    }
}