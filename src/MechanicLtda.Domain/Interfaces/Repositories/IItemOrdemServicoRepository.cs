using MechanicLtda.Domain.Entities;
using MechanicLtda.Infrastructure.Repositories.Base;

namespace MechanicLtda.Domain.Interfaces.Repositories
{
    public interface IItemOrdemServicoRepository : IRepository<ItemOrdemServico>
    {
        Task<IEnumerable<ItemOrdemServico>> ObterPorOrdemServicoIdAsync(int ordemServicoId);
    }
}