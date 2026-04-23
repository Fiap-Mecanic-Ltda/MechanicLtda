using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories.Base;

namespace MechanicLtda.Domain.Interfaces.Repositories
{
    public interface IOrdemServicoRepository : IRepository<OrdemServico>
    {
        Task<IEnumerable<OrdemServico>> ObterPorClienteIdAsync(int clienteId);
    }
}