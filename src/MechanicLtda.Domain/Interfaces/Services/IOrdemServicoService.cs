using MechanicLtda.Domain.Entities;

namespace MechanicLtda.Domain.Interfaces.Services
{
    public interface IOrdemServicoService
    {
        Task<OrdemServico> AdicionarAsync(string descricaoProblema, decimal? valorTotalEstimado, int veiculoId, int clienteId);
        Task<OrdemServico> AtualizarAsync(OrdemServico ordemServico);
        Task<OrdemServico> IniciarDiagnosticoAsync(int id);
        Task<OrdemServico> AguardarAprovacaoAsync(int id);
        Task<OrdemServico> AprovarAsync(int id);
        Task<OrdemServico> RecusarAsync(int id, string motivoRecusa);
        Task<OrdemServico> ConfirmarAprovacaoPorTokenAsync(string token, bool aprovado, string? motivoRecusa);
        Task<OrdemServico> IniciarExecucaoAsync(int id);
        Task<OrdemServico> FinalizarAsync(int id);
        Task<OrdemServico> EntregarAsync(int id);
        Task<IEnumerable<OrdemServico>> ObterTodosAsync();
        Task<(int Quantidade, double MediaMinutos)> ObterTempoMedioExecucaoAsync();
        Task<IEnumerable<OrdemServico>> ObterPorClienteIdAsync(string clienteId);
        Task<IEnumerable<OrdemServico>> ObterPorStatusAsync(string statusDescricao);
        Task<OrdemServico?> ObterPorIdAsync(string id);
        Task RemoverAsync(string id);
    }
}
