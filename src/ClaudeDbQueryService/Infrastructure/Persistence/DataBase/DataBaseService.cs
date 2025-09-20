using Microsoft.EntityFrameworkCore;

namespace ClaudeDbQueryService.Infrastructure.Persistence.DataBase
{
    public class DataBaseService : DbContext, IDataBaseService
    {
        public DataBaseService(DbContextOptions<DataBaseService> options) : base(options)
        {
        }

        public async Task<bool> SaveAsync()
        {
            try
            {
                return await SaveChangesAsync() > 0;
            }
            catch (DbUpdateException ex)
            {
                //var manejadorErrores = new ManejadorErrores();
                throw new Exception(ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("Ocurrió un error inesperado. Por favor, intente nuevamente.");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            EntityConfiguation(modelBuilder);
        }
        private void EntityConfiguation(ModelBuilder modelBuilder)
        {

        }

    }
}
