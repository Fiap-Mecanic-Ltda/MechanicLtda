using MechanicLtda.Domain.Entities;

namespace MechanicLtda.Domain.Interfaces.Services
{
    public interface IClienteService
    {
        Task<Cliente> AdicionarAsync(string nome, string email, string? telefone, string cpfCnpj);
        Task<Cliente> AtualizarAsync(Cliente cliente);
        Task<IEnumerable<Cliente>> ObterTodosAsync();
        Task<Cliente?> ObterPorIdAsync(string id);
        Task RemoverAsync(string id);
    }
}
