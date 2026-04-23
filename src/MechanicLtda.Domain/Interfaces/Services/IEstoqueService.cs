using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;

namespace MechanicLtda.Domain.Interfaces.Services
{
    public interface IEstoqueService
    {
        Task<Estoque> AdicionarAsync(Estoque estoque);
        Task<Estoque> AtualizarAsync(Estoque estoque);
        Task RemoverAsync(string id);
        Task<IEnumerable<Estoque>> ObterTodosAsync();
        Task<IEnumerable<Estoque>> ObterPorTipoAsync(TipoEstoque tipo);
        Task<Estoque?> ObterPorIdAsync(string id);

        /// <summary>
        /// Subtrai quantidade do estoque ao adicionar item em uma OS.
        /// Cria o registro automaticamente com Quantidade = 0 se não existir.
        /// Dispara notificação de baixo estoque quando necessário.
        /// </summary>
        Task<Estoque> SubtrairQuantidadeAsync(int estoqueId, int quantidade);

        /// <summary>
        /// Adiciona quantidade ao estoque (entrada de nota/compra).
        /// </summary>
        Task<Estoque> ReporQuantidadeAsync(int estoqueId, int quantidadeEntrada);
    }
}