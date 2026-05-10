using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories.Base;

namespace MechanicLtda.Domain.Interfaces.Repositories
{
    public interface IOrcamentoRepository : IRepository<Orcamento>
    {
        Task<Orcamento?> ObterPorOrdemServicoIdAsync(int ordemServicoId);
        Task<Orcamento?> ObterComDetalhesAsync(int id);
    }
}