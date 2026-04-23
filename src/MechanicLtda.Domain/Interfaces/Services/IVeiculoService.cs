using MechanicLtda.Domain.Entities;

namespace MechanicLtda.Domain.Interfaces.Services
{
    public interface IVeiculoService
    {
        Task<Veiculo> AdicionarAsync(string placa, string marca, string modelo, int ano, int clienteId);
        Task<Veiculo> AtualizarAsync(Veiculo veiculo);
        Task<IEnumerable<Veiculo>> ObterTodosAsync();
        Task<IEnumerable<Veiculo>> ObterPorClienteIdAsync(string clienteId);
        Task<Veiculo?> ObterPorIdAsync(string id);
        Task RemoverAsync(string id);
    }
}