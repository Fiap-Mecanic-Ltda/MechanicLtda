using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories.Base;

namespace MechanicLtda.Domain.Interfaces.Repositories
{
    public interface IClienteRepository : IRepository<Cliente>
    {
        Task<bool> EmailExisteAsync(string email);
        Task<Cliente?> ObterPorEmailAsync(string email);
    }
}
