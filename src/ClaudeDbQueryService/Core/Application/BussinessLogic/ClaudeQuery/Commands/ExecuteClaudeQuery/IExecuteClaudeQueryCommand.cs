using ClaudeDbQueryService.Core.Application.Configuration;
using ClaudeDbQueryService.Infrastructure.External.Models;
using SincoSoft.MYE.Common.Models;

namespace ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Commands.ExecuteClaudeQuery;

public interface IExecuteClaudeQueryCommand
{
    Task<BaseResponseModel> ExecuteClaudeQuery(QueryQueryRequest request);
}