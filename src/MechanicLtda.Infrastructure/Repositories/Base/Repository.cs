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
            return await _dbSet.FindAsync(id);
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
