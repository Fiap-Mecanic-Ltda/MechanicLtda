using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories.Base;

namespace MechanicLtda.Domain.Interfaces.Repositories
{
    public interface IOrdemServicoAprovacaoTokenRepository : IRepository<OrdemServicoAprovacaoToken>
    {
        Task<OrdemServicoAprovacaoToken?> ObterPorTokenAsync(string token);
    }
}
