using MechanicLtda.Domain.Entities;

namespace MechanicLtda.Domain.Interfaces.Services
{
    public interface IItemOrdemServicoService
    {
        /// <param name="estoqueId">
        /// Quando informado, o serviço realiza a baixa automática no estoque.
        /// </param>
        Task<ItemOrdemServico> AdicionarAsync(int ordemServicoId, int? estoqueId, int quantidade, decimal valorUnitario);
        Task<ItemOrdemServico> AdicionarAsync(int ordemServicoId, int? estoqueId, int? servicoOficinaId, string? descricaoServico, int quantidade, decimal valorUnitario);
        Task<ItemOrdemServico> AtualizarAsync(ItemOrdemServico item);
        Task RemoverAsync(string id);
        Task<IEnumerable<ItemOrdemServico>> ObterPorOrdemServicoIdAsync(int ordemServicoId);
        Task<ItemOrdemServico?> ObterPorIdAsync(string id);
    }
}

