using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories.Base;

namespace MechanicLtda.Domain.Interfaces.Repositories
{
    public interface IItemOrdemServicoRepository : IRepository<ItemOrdemServico>
    {
        Task<IEnumerable<ItemOrdemServico>> ObterPorOrdemServicoIdAsync(int ordemServicoId);
    }
}