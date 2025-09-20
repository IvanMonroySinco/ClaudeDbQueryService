using ClaudeDbQueryService.Core.Application.Configuration;

namespace ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Queries;

public interface IGetHealthStatusQuery
{
    Task<ResponseModel> GetHealthStatus();
}