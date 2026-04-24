using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Infrastructure.Repositories
{
    public class OrcamentoRepository : Repository<Orcamento>, IOrcamentoRepository
    {
        public OrcamentoRepository(BancoAPIContext context) : base(context) { }

        public override async Task<Orcamento?> ObterPorIdAsync(string id)
        {
            return await _dbSet
                .Include(o => o.OrdemServico)
                .FirstOrDefaultAsync(o => o.Id == int.Parse(id));
        }

        public async Task<Orcamento?> ObterPorOrdemServicoIdAsync(int ordemServicoId)
        {
            return await _dbSet
                .Include(o => o.OrdemServico)
                .FirstOrDefaultAsync(o => o.OrdemServicoId == ordemServicoId);
        }
    }
}