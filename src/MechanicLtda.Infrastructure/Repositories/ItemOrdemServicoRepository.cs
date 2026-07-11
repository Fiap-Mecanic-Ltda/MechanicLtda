using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Infrastructure.Repositories
{
    public class ItemOrdemServicoRepository : Repository<ItemOrdemServico>, IItemOrdemServicoRepository
    {
        public ItemOrdemServicoRepository(BancoAPIContext context) : base(context) { }

        public async Task<IEnumerable<ItemOrdemServico>> ObterPorOrdemServicoIdAsync(int ordemServicoId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(i => i.Estoque)
                .Include(i => i.ServicoOficina)
                .Where(i => i.OrdemServicoId == ordemServicoId)
                .ToListAsync();
        }

        public override async Task<ItemOrdemServico?> ObterPorIdAsync(string id)
        {
            var entity = await _dbSet
                .Include(i => i.Estoque)
                .Include(i => i.ServicoOficina)
                .FirstOrDefaultAsync(i => i.Id == int.Parse(id));

            if (entity is not null)
                _context.Entry(entity).State = EntityState.Detached;

            return entity;
        }
    }
}
