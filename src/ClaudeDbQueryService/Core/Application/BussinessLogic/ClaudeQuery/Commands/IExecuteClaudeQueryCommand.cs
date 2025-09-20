using ClaudeDbQueryService.Core.Application.Configuration;
using ClaudeDbQueryService.Infrastructure.External.Models;

namespace ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Commands;

public interface IExecuteClaudeQueryCommand
{
    Task<ResponseModel> ExecuteClaudeQuery(QueryQueryRequest request);
}