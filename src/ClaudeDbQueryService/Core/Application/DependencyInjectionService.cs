using AutoMapper;
using ClaudeDbQueryService.Core.Application.Configuration;
using ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery;
using ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Commands;
using ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Queries;

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
        services.AddTransient<IValidateClaudeQueryQuery, ValidateClaudeQueryQuery>();
        services.AddTransient<IGetHealthStatusQuery, GetHealthStatusQuery>();

        // Register main service
        services.AddTransient<IClaudeQueryService, ClaudeQueryService>();


        return services;
    }
}