using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;

namespace MechanicLtda.Domain.Interfaces.Services
{
    public interface IUsuarioService
    {
        Task<Usuario> AdicionarAsnyc(string userName, string email, TipoUsuario tipo);
        Task<Usuario> AtualizarAsync(Usuario usuario);
        Task<IEnumerable<Usuario>> ObterTodosAsync();
        Task RemoverAsync(string id);
    }
}
