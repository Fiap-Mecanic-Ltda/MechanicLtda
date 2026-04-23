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
                .Where(i => i.OrdemServicoId == ordemServicoId)
                .ToListAsync();
        }

        public override async Task<ItemOrdemServico?> ObterPorIdAsync(string id)
        {
            return await _dbSet.FirstOrDefaultAsync(i => i.Id == int.Parse(id));
        }
    }
}