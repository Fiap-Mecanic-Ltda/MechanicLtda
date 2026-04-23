using MechanicLtda.Domain.Entities;
using MechanicLtda.Infrastructure.Repositories.Base;

namespace MechanicLtda.Domain.Interfaces.Repositories
{
    public interface IClienteRepository : IRepository<Cliente>
    {
        Task<bool> EmailExisteAsync(string email);
        Task<Cliente?> ObterPorEmailAsync(string email);
    }
}
