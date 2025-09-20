namespace ClaudeDbQueryService.Core.Application.Configuration;

public class ResponseModel
{
    public bool IsSuccess { get; set; }
    public object? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}