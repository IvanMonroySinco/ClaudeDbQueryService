using ClaudeDbQueryService.Core.Application.Configuration;

namespace ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Queries;

public interface IValidateClaudeQueryQuery
{
    Task<ResponseModel> ValidateQuery(string query);
}