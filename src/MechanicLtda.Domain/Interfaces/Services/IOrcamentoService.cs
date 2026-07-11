using MechanicLtda.Domain.Entities;

namespace MechanicLtda.Domain.Interfaces.Services
{
    public interface IOrcamentoService
    {
        Task<Orcamento> CriarOuAtualizarAsync(int ordemServicoId);
        Task<Orcamento> AtualizarManualAsync(Orcamento orcamento);
        Task<Orcamento?> ObterPorIdAsync(string id);
        Task<Orcamento?> ObterPorOrdemServicoIdAsync(int ordemServicoId);
        Task<Orcamento?> ObterComDetalhesAsync(int id);
        Task RemoverAsync(string id);
    }
}