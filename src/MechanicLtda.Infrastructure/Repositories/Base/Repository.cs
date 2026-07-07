using MechanicLtda.Domain.Interfaces.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Infrastructure.Repositories.Base
{
    public abstract class Repository<TEntity> : IRepository<TEntity> where TEntity : class, new()
    {
        protected readonly BancoAPIContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        protected Repository(BancoAPIContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }

        public virtual async Task<TEntity> AdicionarAsync(TEntity entity)
        {
            var newEntity = _dbSet.Add(entity);
            await SaveChanges();
            return newEntity.Entity;
        }

        public virtual async Task<TEntity> AtualizarAsync(TEntity entity)
        {
            var updatedEntity = _dbSet.Update(entity);
            await SaveChanges();
            return updatedEntity.Entity;
        }

        public virtual async Task RemoverAsync(string id)
        {
            var entity = await ObterPorIdAsync(id);

            if (entity is not null)
            {
                _dbSet.Remove(entity);
                await SaveChanges();
            }
        }

        public virtual async Task<IEnumerable<TEntity>> ObterTodosAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public virtual async Task<TEntity?> ObterPorIdAsync(string id)
        {
            // Obter a chave primária
            var keyProperty = _context.Model.FindEntityType(typeof(TEntity))?.FindPrimaryKey()?.Properties.FirstOrDefault();

            if (keyProperty == null)
                return null;

            // Se a chave é int, converter a string para int
            if (keyProperty.ClrType == typeof(int) && int.TryParse(id, out var intId))
                return await _dbSet.FindAsync(intId);

            // Se a chave é string, usar direto
            if (keyProperty.ClrType == typeof(string))
                return await _dbSet.FindAsync(id);

            // Para outros tipos, tentar converter
            try
            {
                var convertedId = Convert.ChangeType(id, keyProperty.ClrType);
                return await _dbSet.FindAsync(convertedId);
            }
            catch
            {
                return null;
            }
        }

        public async Task<int> SaveChanges()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
