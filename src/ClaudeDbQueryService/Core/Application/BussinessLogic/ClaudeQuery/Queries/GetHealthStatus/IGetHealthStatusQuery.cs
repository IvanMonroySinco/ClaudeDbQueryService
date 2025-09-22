using ClaudeDbQueryService.Core.Application.Configuration;
using SincoSoft.MYE.Common.Models;

namespace ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Queries.GetHealthStatus;

public interface IGetHealthStatusQuery
{
    Task<BaseResponseModel> GetHealthStatus(); 
}