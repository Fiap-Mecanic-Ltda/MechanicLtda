using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;

namespace MechanicLtda.Domain.Interfaces.Services
{
    /// <summary>
    /// Eventos de negócio que alimentam os painéis e alertas de observabilidade: volume de
    /// ordens de serviço, tempo em cada status, falhas de processamento e de integrações.
    ///
    /// O domínio só descreve o que aconteceu; a implementação decide para onde o evento
    /// vai. Nenhuma chamada desta interface pode interromper o fluxo de negócio — monitorar
    /// é consequência da operação, não parte dela.
    /// </summary>
    public interface IMonitoramentoService
    {
        void RegistrarOrdemServicoCriada(OrdemServico ordemServico);

        void RegistrarMudancaStatus(OrdemServico ordemServico, StatusOrdemServico statusAnterior, TimeSpan tempoNoStatusAnterior);

        void RegistrarFalhaProcessamento(string operacao, int? ordemServicoId, Exception excecao);

        void RegistrarFalhaIntegracao(string integracao, string operacao, Exception excecao);
    }
}
