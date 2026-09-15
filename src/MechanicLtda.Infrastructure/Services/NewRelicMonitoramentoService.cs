using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;
using MechanicLtda.Domain.Interfaces.Services;
using MechanicLtda.Domain.Services;
using Microsoft.Extensions.Logging;
using NewRelicAgent = NewRelic.Api.Agent.NewRelic;

namespace MechanicLtda.Infrastructure.Services
{
    /// <summary>
    /// Envia os eventos de negócio para o New Relic como custom events, que alimentam os
    /// painéis (NRQL) e os alertas:
    ///
    /// - <c>OrdemServicoEvento</c> (tipo Criada / MudancaStatus) — volume diário de OS e
    ///   tempo médio em cada status;
    /// - <c>OrdemServicoFalha</c> — falhas no processamento das OS, classificadas em
    ///   Negocio ou Sistema (só Sistema alerta);
    /// - <c>FalhaIntegracao</c> — falhas em integrações externas, como o SMTP.
    ///
    /// Sem o agente carregado no processo (testes, execução local), a API do New Relic não
    /// faz nada. Cada evento também sai no log estruturado, para continuar visível sem o
    /// agente.
    /// </summary>
    public class NewRelicMonitoramentoService : IMonitoramentoService
    {
        private const int TamanhoMaximoMensagem = 255;

        private readonly ILogger<NewRelicMonitoramentoService> _logger;

        public NewRelicMonitoramentoService(ILogger<NewRelicMonitoramentoService> logger)
        {
            _logger = logger;
        }

        public void RegistrarOrdemServicoCriada(OrdemServico ordemServico)
        {
            Registrar("OrdemServicoEvento", new Dictionary<string, object>
            {
                ["tipo"]            = "Criada",
                ["ordemServicoId"]  = ordemServico.Id,
                ["status"]          = ordemServico.Status.ToString(),
                ["statusDescricao"] = ordemServico.Status.Descricao(),
            });
        }

        public void RegistrarMudancaStatus(OrdemServico ordemServico, StatusOrdemServico statusAnterior, TimeSpan tempoNoStatusAnterior)
        {
            Registrar("OrdemServicoEvento", new Dictionary<string, object>
            {
                ["tipo"]                    = "MudancaStatus",
                ["ordemServicoId"]          = ordemServico.Id,
                ["statusAnterior"]          = statusAnterior.ToString(),
                ["statusAnteriorDescricao"] = statusAnterior.Descricao(),
                ["statusNovo"]              = ordemServico.Status.ToString(),
                ["statusNovoDescricao"]     = ordemServico.Status.Descricao(),
                ["minutosNoStatusAnterior"] = Math.Round(tempoNoStatusAnterior.TotalMinutes, 2),
            });
        }

        public void RegistrarFalhaProcessamento(string operacao, int? ordemServicoId, Exception excecao)
        {
            var categoria = ClassificacaoFalha.Classificar(excecao);

            Registrar("OrdemServicoFalha", new Dictionary<string, object>
            {
                ["operacao"]       = operacao,
                ["ordemServicoId"] = ordemServicoId ?? 0,
                ["categoria"]      = categoria,
                ["erro"]           = excecao.GetType().Name,
                ["mensagem"]       = Truncar(excecao.Message),
            });

            // Violação de regra (ex.: finalizar OS que não está em execução) já vira 400 para
            // quem chamou; marcar como erro no APM poluiria a taxa de erro e os alertas.
            if (categoria == ClassificacaoFalha.Sistema)
                NotificarErro(excecao, operacao);
        }

        public void RegistrarFalhaIntegracao(string integracao, string operacao, Exception excecao)
        {
            Registrar("FalhaIntegracao", new Dictionary<string, object>
            {
                ["integracao"] = integracao,
                ["operacao"]   = operacao,
                ["erro"]       = excecao.GetType().Name,
                ["mensagem"]   = Truncar(excecao.Message),
            });

            NotificarErro(excecao, $"{integracao}:{operacao}");
        }

        private void Registrar(string tipoEvento, Dictionary<string, object> atributos)
        {
            _logger.LogInformation("Evento de monitoramento {TipoEvento} {@Atributos}", tipoEvento, atributos);

            try
            {
                NewRelicAgent.RecordCustomEvent(tipoEvento, atributos);
            }
            catch (Exception ex)
            {
                // Monitoramento nunca derruba a operação de negócio.
                _logger.LogWarning(ex, "Falha ao registrar o evento {TipoEvento} no New Relic.", tipoEvento);
            }
        }

        private void NotificarErro(Exception excecao, string operacao)
        {
            try
            {
                NewRelicAgent.NoticeError(excecao, new Dictionary<string, string> { ["operacao"] = operacao });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao notificar o erro da operação {Operacao} no New Relic.", operacao);
            }
        }

        // Custom events do New Relic truncam atributos longos; cortar aqui mantém o
        // começo da mensagem, que é o que identifica o problema.
        private static string Truncar(string mensagem) =>
            mensagem.Length <= TamanhoMaximoMensagem ? mensagem : mensagem[..TamanhoMaximoMensagem];
    }
}
