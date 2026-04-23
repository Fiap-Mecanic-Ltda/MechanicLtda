using MechanicLtda.Domain.Entities;

namespace MechanicLtda.Domain.Interfaces.Services
{
    public interface IOrdemServicoService
    {
        Task<OrdemServico> AdicionarAsync(string descricaoProblema, decimal? valorTotalEstimado, int veiculoId, int clienteId);
        Task<OrdemServico> AtualizarAsync(OrdemServico ordemServico);
        Task<OrdemServico> MoverParaEmValidacaoAsync(int id);
        Task<IEnumerable<OrdemServico>> ObterTodosAsync();
        Task<IEnumerable<OrdemServico>> ObterPorClienteIdAsync(string clienteId);
        Task<OrdemServico?> ObterPorIdAsync(string id);
        Task RemoverAsync(string id);
    }
}