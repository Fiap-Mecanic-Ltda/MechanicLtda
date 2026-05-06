using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Infrastructure.Repositories
{
    public class OrdemServicoRepository : Repository<OrdemServico>, IOrdemServicoRepository
    {
        public OrdemServicoRepository(BancoAPIContext context) : base(context) { }

        public override async Task<IEnumerable<OrdemServico>> ObterTodosAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(o => o.Veiculo)
                .Include(o => o.Cliente)
                .ToListAsync();
        }

        public override async Task<OrdemServico?> ObterPorIdAsync(string id)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(o => o.Veiculo)
                .Include(o => o.Cliente)
                .FirstOrDefaultAsync(o => o.Id == int.Parse(id));
        }

        public async Task<IEnumerable<OrdemServico>> ObterPorClienteIdAsync(int clienteId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(o => o.Veiculo)
                .Include(o => o.Cliente)
                .Where(o => o.ClienteId == clienteId)
                .ToListAsync();
        }
    }
}