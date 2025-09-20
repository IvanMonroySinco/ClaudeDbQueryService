using ClaudeDbQueryService.Core.Application.Configuration;
using ClaudeDbQueryService.Infrastructure.External.Models;

namespace ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery;

public interface IClaudeQueryService
{
    Task<ResponseModel> ProcessQuery(QueryQueryRequest request);
    Task<ResponseModel> GetHealthStatus();
}