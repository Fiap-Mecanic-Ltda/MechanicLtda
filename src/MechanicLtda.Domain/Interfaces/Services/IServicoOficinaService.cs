using MechanicLtda.Domain.Entities;

namespace MechanicLtda.Domain.Interfaces.Services
{
    public interface IServicoOficinaService
    {
        Task<ServicoOficina> AdicionarAsync(ServicoOficina servico);
        Task<ServicoOficina> AtualizarAsync(ServicoOficina servico);
        Task RemoverAsync(string id);
        Task<IEnumerable<ServicoOficina>> ObterTodosAsync();
        Task<IEnumerable<ServicoOficina>> ObterAtivosAsync();
        Task<ServicoOficina?> ObterPorIdAsync(string id);
    }
}
