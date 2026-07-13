using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Infrastructure.Repositories
{
    public class OrdemServicoAprovacaoTokenRepository : Repository<OrdemServicoAprovacaoToken>, IOrdemServicoAprovacaoTokenRepository
    {
        public OrdemServicoAprovacaoTokenRepository(BancoAPIContext context) : base(context) { }

        public async Task<OrdemServicoAprovacaoToken?> ObterPorTokenAsync(string token)
        {
            return await _dbSet
                .Include(t => t.OrdemServico)
                    .ThenInclude(os => os.Cliente)
                .FirstOrDefaultAsync(t => t.Token == token);
        }
    }
}
