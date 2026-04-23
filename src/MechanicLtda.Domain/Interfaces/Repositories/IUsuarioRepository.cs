using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories.Base;

namespace MechanicLtda.Domain.Interfaces.Repositories
{
    public interface IUsuarioRepository : IRepository<Usuario>
    {
        Task<bool> EmailExisteAsync(string email);
        Task<Usuario?> ObterPorEmailAsync(string email);
    }
}
