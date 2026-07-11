using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories.Base;

namespace MechanicLtda.Domain.Interfaces.Repositories
{
    public interface IServicoOficinaRepository : IRepository<ServicoOficina>
    {
        Task<IEnumerable<ServicoOficina>> ObterAtivosAsync();
        Task<ServicoOficina?> ObterPorNomeAsync(string nome);
    }
}
