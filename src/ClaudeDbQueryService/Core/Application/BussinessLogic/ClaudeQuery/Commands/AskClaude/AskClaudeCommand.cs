using ClaudeDbQueryService.Infrastructure.External.Models;
using SincoSoft.MYE.Common.Models;
using Serilog;
using ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Commands.ExecuteClaudeQuery;

namespace ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Commands.AskClaude;

public class AskClaudeCommand : IAskClaudeCommand
{
    private readonly IExecuteClaudeQueryCommand _executeCommand;

    public AskClaudeCommand(IExecuteClaudeQueryCommand executeCommand)
    {
        _executeCommand = executeCommand;
    }


    public async Task<BaseResponseModel> ProcessQuery(QueryQueryRequest request)
    {
        var response = new BaseResponseModel();
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                Log.Error("Query cannot be empty");
                response.StatusCode = 400;
                response.Message = "La consulta no puede estar vacia.";
                return response;
            }

            if (request.Query.Length > 10000)
            {
                Log.Error("Query is too long");
                response.StatusCode = 400;
                response.Message = "La consulta debe ser menos a 10,000 caracteres";
                return response;
            }

            return await _executeCommand.ExecuteClaudeQuery(request);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing query: {Query}", request.Query);
            response.Success = false;
            response.StatusCode = 400;
            response.Message = "Se ha producido un error al procesar la solicitud. Por favor, inténtelo nuevamente más tarde.";
            return response;

        }
    }

}