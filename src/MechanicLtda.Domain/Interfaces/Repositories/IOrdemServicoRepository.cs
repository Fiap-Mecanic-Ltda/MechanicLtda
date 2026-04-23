using MechanicLtda.Domain.Entities;
using MechanicLtda.Infrastructure.Repositories.Base;

namespace MechanicLtda.Domain.Interfaces.Repositories
{
    public interface IOrdemServicoRepository : IRepository<OrdemServico>
    {
        Task<IEnumerable<OrdemServico>> ObterPorClienteIdAsync(int clienteId);
    }
}