namespace ClaudeDbQueryService.Infrastructure.Persistence.DataBase
{
    public interface IDataBaseService
    {
        Task<bool> SaveAsync();
    }
}
