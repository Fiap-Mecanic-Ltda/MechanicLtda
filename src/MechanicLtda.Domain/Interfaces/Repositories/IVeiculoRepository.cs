using MechanicLtda.Domain.Entities;
using MechanicLtda.Infrastructure.Repositories.Base;

namespace MechanicLtda.Domain.Interfaces.Repositories
{
    public interface IVeiculoRepository : IRepository<Veiculo>
    {
        Task<bool> PlacaExisteAsync(string placa);
        Task<IEnumerable<Veiculo>> ObterPorClienteIdAsync(int clienteId);
    }
}