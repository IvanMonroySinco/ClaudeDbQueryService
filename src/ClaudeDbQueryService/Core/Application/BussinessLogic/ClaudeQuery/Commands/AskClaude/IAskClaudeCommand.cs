using ClaudeDbQueryService.Core.Application.Configuration;
using ClaudeDbQueryService.Infrastructure.External.Models;
using SincoSoft.MYE.Common.Models;

namespace ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Commands.AskClaude;

public interface IAskClaudeCommand
{
    Task<BaseResponseModel> ProcessQuery(QueryQueryRequest request);
}