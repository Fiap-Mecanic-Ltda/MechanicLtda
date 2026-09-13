using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Domain.Interfaces.Services;
using MechanicLtda.Domain.Services.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace MechanicLtda.Domain.Services
{
    public class OrdemServicoService : BaseService, IOrdemServicoService
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly IVeiculoRepository _veiculoRepository;
        private readonly IOrdemServicoAprovacaoTokenRepository _ordemServicoAprovacaoTokenRepository;
        private readonly IEmailService _emailService;
        private readonly IMonitoramentoService _monitoramentoService;
        private readonly ILogger<OrdemServicoService> _logger;

        public OrdemServicoService(ILogger<OrdemServicoService> logger,
                                   IConfiguration configuration,
                                   INotificadorService notificadorService,
                                   IOrdemServicoRepository ordemServicoRepository,
                                   IVeiculoRepository veiculoRepository,
                                   IOrdemServicoAprovacaoTokenRepository ordemServicoAprovacaoTokenRepository,
                                   IEmailService emailService,
                                   IMonitoramentoService monitoramentoService) : base(notificadorService, configuration)
        {
            _ordemServicoRepository = ordemServicoRepository;
            _veiculoRepository = veiculoRepository;
            _ordemServicoAprovacaoTokenRepository = ordemServicoAprovacaoTokenRepository;
            _emailService = emailService;
            _monitoramentoService = monitoramentoService;
            _logger = logger;
        }

        public async Task<OrdemServico> AdicionarAsync(string descricaoProblema, decimal? valorTotalEstimado, int veiculoId, int clienteId)
        {
            try
            {
                // Converter string para int para buscar o veículo
                if (!int.TryParse(veiculoId.ToString(), out var veiculoIdInt))
                    throw new ArgumentException($"VeiculoId inválido: {veiculoId}");

                var veiculo = await _veiculoRepository.ObterPorIdAsync(veiculoId.ToString())
                    ?? throw new KeyNotFoundException($"Veículo com Id '{veiculoId}' não encontrado.");

                if (veiculo.ClienteId != clienteId)
                    throw new InvalidOperationException("O veículo informado não pertence ao cliente indicado.");

                var ordemServico = new OrdemServico
                {
                    DescricaoProblema  = descricaoProblema,
                    ValorTotalEstimado = valorTotalEstimado,
                    VeiculoId          = veiculoId,
                    ClienteId          = clienteId,
                    Status              = StatusOrdemServico.Recebida,
                    DataCriacao         = DateTime.Now,
                    DataAlteracaoStatus = DateTime.UtcNow
                };

                var novaOrdem = await _ordemServicoRepository.AdicionarAsync(ordemServico);

                var resultado = await _ordemServicoRepository.ObterPorIdAsync(novaOrdem.Id.ToString()) ?? novaOrdem;
                _monitoramentoService.RegistrarOrdemServicoCriada(resultado);

                await EnviarEmailNotificacaoStatusAsync(resultado);

                return resultado;
            }
            catch (Exception ex)
            {
                _monitoramentoService.RegistrarFalhaProcessamento("Adicionar", null, ex);
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:AdicionarAsync", _logger);
                throw;
            }
        }

        public async Task<OrdemServico> AtualizarAsync(OrdemServico ordemServico)
        {
            try
            {
                var existente = await _ordemServicoRepository.ObterPorIdAsync(ordemServico.Id.ToString())
                    ?? throw new KeyNotFoundException($"Ordem de Serviço com Id '{ordemServico.Id}' não encontrada.");

                var veiculo = await _veiculoRepository.ObterPorIdAsync(ordemServico.VeiculoId.ToString())
                    ?? throw new KeyNotFoundException($"Veículo com Id '{ordemServico.VeiculoId}' não encontrado.");

                if (veiculo.ClienteId != ordemServico.ClienteId)
                    throw new InvalidOperationException("O veículo informado não pertence ao cliente indicado.");

                var statusAnterior       = existente.Status;
                var inicioStatusAnterior = InicioDoStatusAtual(existente);
                var agora                = DateTime.UtcNow;

                ordemServico.Status          = existente.Status;
                ordemServico.DataCriacao     = existente.DataCriacao;
                ordemServico.DataModificacao = agora;
                ordemServico.Cliente         = existente.Cliente;

                // O DTO de atualização não traz as datas de ciclo de vida, e o Update do EF
                // grava todas as colunas: sem copiá-las, editar uma OS em execução apagaria
                // o início e o fim da execução (e, com eles, o tempo médio de execução).
                ordemServico.DataInicioExecucao  = existente.DataInicioExecucao;
                ordemServico.DataFimExecucao     = existente.DataFimExecucao;
                ordemServico.DataAlteracaoStatus = existente.DataAlteracaoStatus;

                // Gatilho: ao preencher descrição + valor estimado na OS recebida, avança para Em Diagnóstico
                if (!string.IsNullOrWhiteSpace(ordemServico.DescricaoProblema) &&
                    ordemServico.ValorTotalEstimado.HasValue &&
                    existente.Status == StatusOrdemServico.Recebida)
                {
                    ordemServico.Status              = StatusOrdemServico.EmDiagnostico;
                    ordemServico.DataAlteracaoStatus = agora;
                }

                var resultado = await _ordemServicoRepository.AtualizarAsync(ordemServico);

                if (resultado.Status != statusAnterior)
                {
                    RegistrarTransicao(resultado, statusAnterior, inicioStatusAnterior, agora);
                    await EnviarEmailNotificacaoStatusAsync(resultado);
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _monitoramentoService.RegistrarFalhaProcessamento("Atualizar", ordemServico.Id, ex);
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:AtualizarAsync", _logger);
                throw;
            }
        }

        public async Task<OrdemServico> IniciarDiagnosticoAsync(int id)
        {
            try
            {
                var ordemServico = await _ordemServicoRepository.ObterPorIdAsync(id.ToString())
                    ?? throw new KeyNotFoundException($"Ordem de Serviço com Id '{id}' não encontrada.");

                if (ordemServico.Status != StatusOrdemServico.Recebida)
                    throw new InvalidOperationException($"A OS só pode ir para 'Em Diagnóstico' quando estiver 'Recebida'. Status atual: {ordemServico.Status}.");

                var statusAnterior       = ordemServico.Status;
                var inicioStatusAnterior = InicioDoStatusAtual(ordemServico);
                var agora                = DateTime.UtcNow;

                ordemServico.Status              = StatusOrdemServico.EmDiagnostico;
                ordemServico.DataModificacao     = agora;
                ordemServico.DataAlteracaoStatus = agora;

                var resultado = await _ordemServicoRepository.AtualizarAsync(ordemServico);

                RegistrarTransicao(resultado, statusAnterior, inicioStatusAnterior, agora);

                await EnviarEmailNotificacaoStatusAsync(resultado);

                return resultado;
            }
            catch (Exception ex)
            {
                _monitoramentoService.RegistrarFalhaProcessamento("IniciarDiagnostico", id, ex);
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:IniciarDiagnosticoAsync", _logger);
                throw;
            }
        }

        public async Task<OrdemServico> AguardarAprovacaoAsync(int id)
        {
            try
            {
                var ordemServico = await _ordemServicoRepository.ObterPorIdAsync(id.ToString())
                    ?? throw new KeyNotFoundException($"Ordem de Serviço com Id '{id}' não encontrada.");

                if (ordemServico.Status != StatusOrdemServico.EmDiagnostico)
                    throw new InvalidOperationException($"A OS só pode ir para 'Aguardando Aprovação' quando estiver 'Em Diagnóstico'. Status atual: {ordemServico.Status}.");

                var statusAnterior       = ordemServico.Status;
                var inicioStatusAnterior = InicioDoStatusAtual(ordemServico);
                var agora                = DateTime.UtcNow;

                ordemServico.Status              = StatusOrdemServico.AguardandoAprovacao;
                ordemServico.DataModificacao     = agora;
                ordemServico.DataAlteracaoStatus = agora;

                var resultado = await _ordemServicoRepository.AtualizarAsync(ordemServico);

                RegistrarTransicao(resultado, statusAnterior, inicioStatusAnterior, agora);

                await EnviarEmailNotificacaoStatusAsync(resultado);
                await EnviarEmailAprovacaoAsync(resultado);

                return resultado;
            }
            catch (Exception ex)
            {
                _monitoramentoService.RegistrarFalhaProcessamento("AguardarAprovacao", id, ex);
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:AguardarAprovacaoAsync", _logger);
                throw;
            }
        }

        private async Task EnviarEmailNotificacaoStatusAsync(OrdemServico ordemServico)
        {
            try
            {
                if (ordemServico.Cliente == null || string.IsNullOrWhiteSpace(ordemServico.Cliente.Email))
                {
                    _logger.LogWarning("OS {Id}: cliente sem e-mail cadastrado, notificação de status não enviada.", ordemServico.Id);
                    return;
                }

                var statusDescricao = GetStatusDescription(ordemServico.Status);

                var corpoHtml = $"""
                    <p>Olá, {ordemServico.Cliente.Nome}.</p>
                    <p>Sua Ordem de Serviço #{ordemServico.Id} teve o status atualizado para: <strong>{statusDescricao}</strong>.</p>
                    """;

                await _emailService.EnviarEmailAsync(ordemServico.Cliente.Email, ordemServico.Cliente.Nome,
                    $"Atualização da Ordem de Serviço #{ordemServico.Id}", corpoHtml);
            }
            catch (Exception ex)
            {
                _monitoramentoService.RegistrarFalhaIntegracao("Email", "NotificacaoStatus", ex);
                _logger.LogError(ex, "Falha ao enviar e-mail de notificação de status para a OS {Id}. A transição de status não foi afetada.", ordemServico.Id);
            }
        }

        private async Task EnviarEmailAprovacaoAsync(OrdemServico ordemServico)
        {
            try
            {
                if (ordemServico.Cliente == null || string.IsNullOrWhiteSpace(ordemServico.Cliente.Email))
                {
                    _logger.LogWarning("OS {Id}: cliente sem e-mail cadastrado, notificação de aprovação não enviada.", ordemServico.Id);
                    return;
                }

                var horasExpiracao = _configuration.GetValue<int?>("AppSettings:AprovacaoTokenExpiracaoHoras") ?? 72;

                var token = new OrdemServicoAprovacaoToken
                {
                    OrdemServicoId = ordemServico.Id,
                    Token          = GerarTokenSeguro(),
                    DataCriacao    = DateTime.UtcNow,
                    DataExpiracao  = DateTime.UtcNow.AddHours(horasExpiracao),
                    Utilizado      = false
                };

                await _ordemServicoAprovacaoTokenRepository.AdicionarAsync(token);

                var baseUrl = _configuration["AppSettings:BaseUrlAprovacao"]?.TrimEnd('/')
                    ?? throw new InvalidOperationException("AppSettings:BaseUrlAprovacao não configurado.");

                // Caminho em minúsculas de propósito: no API Gateway o roteamento
                // diferencia maiúsculas, e a rota pública é
                // "GET /api/aprovacaoordemservico/{token}/aprovar". Com
                // "AprovacaoOrdemServico" o link cairia em "ANY /api/{proxy+}",
                // que exige token, e o cliente receberia 401 ao clicar no e-mail.
                var linkAprovar = $"{baseUrl}/api/aprovacaoordemservico/{token.Token}/aprovar";
                var linkRecusar = $"{baseUrl}/api/aprovacaoordemservico/{token.Token}/recusar";

                var corpoHtml = $"""
                    <p>Olá, {ordemServico.Cliente.Nome}.</p>
                    <p>Sua Ordem de Serviço #{ordemServico.Id} está aguardando sua aprovação.</p>
                    <p><a href="{linkAprovar}">Aprovar Ordem de Serviço</a></p>
                    <p><a href="{linkRecusar}">Recusar Ordem de Serviço</a></p>
                    <p>Este link expira em {horasExpiracao} horas.</p>
                    """;

                await _emailService.EnviarEmailAsync(ordemServico.Cliente.Email, ordemServico.Cliente.Nome,
                    "Aprovação de Ordem de Serviço pendente", corpoHtml);
            }
            catch (Exception ex)
            {
                _monitoramentoService.RegistrarFalhaIntegracao("Email", "Aprovacao", ex);
                _logger.LogError(ex, "Falha ao enviar e-mail de aprovação para a OS {Id}. A transição de status não foi afetada.", ordemServico.Id);
            }
        }

        private static string GerarTokenSeguro()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        public async Task<OrdemServico> AprovarAsync(int id)
        {
            try
            {
                var ordemServico = await _ordemServicoRepository.ObterPorIdAsync(id.ToString())
                    ?? throw new KeyNotFoundException($"Ordem de Serviço com Id '{id}' não encontrada.");

                if (ordemServico.Status != StatusOrdemServico.AguardandoAprovacao)
                    throw new InvalidOperationException($"A OS só pode ser aprovada quando estiver 'Aguardando Aprovação'. Status atual: {ordemServico.Status}.");

                var statusAnterior       = ordemServico.Status;
                var inicioStatusAnterior = InicioDoStatusAtual(ordemServico);
                var agora                = DateTime.UtcNow;

                ordemServico.Status              = StatusOrdemServico.EmExecucao;
                ordemServico.DataModificacao     = agora;
                ordemServico.DataAlteracaoStatus = agora;

                var resultado = await _ordemServicoRepository.AtualizarAsync(ordemServico);

                RegistrarTransicao(resultado, statusAnterior, inicioStatusAnterior, agora);

                await EnviarEmailNotificacaoStatusAsync(resultado);

                return resultado;
            }
            catch (Exception ex)
            {
                _monitoramentoService.RegistrarFalhaProcessamento("Aprovar", id, ex);
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:AprovarAsync", _logger);
                throw;
            }
        }

        public async Task<OrdemServico> RecusarAsync(int id, string motivoRecusa)
        {
            try
            {
                var ordemServico = await _ordemServicoRepository.ObterPorIdAsync(id.ToString())
                    ?? throw new KeyNotFoundException($"Ordem de Serviço com Id '{id}' não encontrada.");

                if (ordemServico.Status != StatusOrdemServico.AguardandoAprovacao)
                    throw new InvalidOperationException($"A OS só pode ser recusada quando estiver 'Aguardando Aprovação'. Status atual: {ordemServico.Status}.");

                var statusAnterior       = ordemServico.Status;
                var inicioStatusAnterior = InicioDoStatusAtual(ordemServico);
                var agora                = DateTime.UtcNow;

                // Voltar para Em Diagnóstico para revisão
                ordemServico.Status              = StatusOrdemServico.EmDiagnostico;
                ordemServico.DataModificacao     = agora;
                ordemServico.DataAlteracaoStatus = agora;

                // Adicionar motivo da recusa na descrição ou em outro campo se disponível
                if (!string.IsNullOrWhiteSpace(motivoRecusa))
                    ordemServico.DescricaoProblema = $"{ordemServico.DescricaoProblema}\n[RECUSA]: {motivoRecusa}";

                var resultado = await _ordemServicoRepository.AtualizarAsync(ordemServico);

                RegistrarTransicao(resultado, statusAnterior, inicioStatusAnterior, agora);

                await EnviarEmailNotificacaoStatusAsync(resultado);

                return resultado;
            }
            catch (Exception ex)
            {
                _monitoramentoService.RegistrarFalhaProcessamento("Recusar", id, ex);
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:RecusarAsync", _logger);
                throw;
            }
        }

        public async Task<OrdemServico> IniciarExecucaoAsync(int id)
        {
            try
            {
                var ordemServico = await _ordemServicoRepository.ObterPorIdAsync(id.ToString())
                    ?? throw new KeyNotFoundException($"Ordem de Serviço com Id '{id}' não encontrada.");

                if (ordemServico.Status != StatusOrdemServico.AguardandoAprovacao)
                    throw new InvalidOperationException($"A OS só pode ir para 'Em Execução' quando estiver 'Aguardando Aprovação'. Status atual: {ordemServico.Status}.");

                var statusAnterior       = ordemServico.Status;
                var inicioStatusAnterior = InicioDoStatusAtual(ordemServico);
                var agora                = DateTime.UtcNow;

                ordemServico.Status              = StatusOrdemServico.EmExecucao;
                ordemServico.DataInicioExecucao  = agora;
                ordemServico.DataModificacao     = agora;
                ordemServico.DataAlteracaoStatus = agora;

                var resultado = await _ordemServicoRepository.AtualizarAsync(ordemServico);

                RegistrarTransicao(resultado, statusAnterior, inicioStatusAnterior, agora);

                await EnviarEmailNotificacaoStatusAsync(resultado);

                return resultado;
            }
            catch (Exception ex)
            {
                _monitoramentoService.RegistrarFalhaProcessamento("IniciarExecucao", id, ex);
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:IniciarExecucaoAsync", _logger);
                throw;
            }
        }

        public async Task<OrdemServico> FinalizarAsync(int id)
        {
            try
            {
                var ordemServico = await _ordemServicoRepository.ObterPorIdAsync(id.ToString())
                    ?? throw new KeyNotFoundException($"Ordem de Serviço com Id '{id}' não encontrada.");

                if (ordemServico.Status != StatusOrdemServico.EmExecucao)
                    throw new InvalidOperationException($"A OS só pode ser 'Finalizada' quando estiver 'Em Execução'. Status atual: {ordemServico.Status}.");

                var statusAnterior       = ordemServico.Status;
                var inicioStatusAnterior = InicioDoStatusAtual(ordemServico);
                var agora                = DateTime.UtcNow;

                ordemServico.Status              = StatusOrdemServico.Finalizada;
                ordemServico.DataFimExecucao     = agora;
                ordemServico.DataModificacao     = agora;
                ordemServico.DataAlteracaoStatus = agora;

                var resultado = await _ordemServicoRepository.AtualizarAsync(ordemServico);

                RegistrarTransicao(resultado, statusAnterior, inicioStatusAnterior, agora);

                await EnviarEmailNotificacaoStatusAsync(resultado);

                return resultado;
            }
            catch (Exception ex)
            {
                _monitoramentoService.RegistrarFalhaProcessamento("Finalizar", id, ex);
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:FinalizarAsync", _logger);
                throw;
            }
        }

        public async Task<OrdemServico> EntregarAsync(int id)
        {
            try
            {
                var ordemServico = await _ordemServicoRepository.ObterPorIdAsync(id.ToString())
                    ?? throw new KeyNotFoundException($"Ordem de Serviço com Id '{id}' não encontrada.");

                if (ordemServico.Status != StatusOrdemServico.Finalizada)
                    throw new InvalidOperationException($"A OS só pode ser 'Entregue' quando estiver 'Finalizada'. Status atual: {ordemServico.Status}.");

                var statusAnterior       = ordemServico.Status;
                var inicioStatusAnterior = InicioDoStatusAtual(ordemServico);
                var agora                = DateTime.UtcNow;

                ordemServico.Status              = StatusOrdemServico.Entregue;
                ordemServico.DataModificacao     = agora;
                ordemServico.DataAlteracaoStatus = agora;

                var resultado = await _ordemServicoRepository.AtualizarAsync(ordemServico);

                RegistrarTransicao(resultado, statusAnterior, inicioStatusAnterior, agora);

                await EnviarEmailNotificacaoStatusAsync(resultado);

                return resultado;
            }
            catch (Exception ex)
            {
                _monitoramentoService.RegistrarFalhaProcessamento("Entregar", id, ex);
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:EntregarAsync", _logger);
                throw;
            }
        }

        // Ordem de prioridade da listagem geral: OS mais "urgentes" (em andamento)
        // primeiro. Não é a ordem numérica do enum (Recebida=1 .. Entregue=6).
        private static readonly Dictionary<StatusOrdemServico, int> _prioridadeListagem = new()
        {
            [StatusOrdemServico.EmExecucao]         = 1,
            [StatusOrdemServico.AguardandoAprovacao] = 2,
            [StatusOrdemServico.EmDiagnostico]      = 3,
            [StatusOrdemServico.Recebida]           = 4,
        };

        public async Task<IEnumerable<OrdemServico>> ObterTodosAsync()
        {
            try
            {
                var todas = await _ordemServicoRepository.ObterTodosAsync();

                // Exclusão lógica: OS finalizadas/entregues não aparecem na listagem
                // geral (continuam no banco e acessíveis via ObterPorIdAsync).
                return todas
                    .Where(os => os.Status != StatusOrdemServico.Finalizada
                              && os.Status != StatusOrdemServico.Entregue)
                    .OrderBy(os => _prioridadeListagem.GetValueOrDefault(os.Status, int.MaxValue))
                    .ThenBy(os => os.DataCriacao)
                    .ToList();
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:ObterTodosAsync", _logger);
                throw;
            }
        }

        public async Task<(int Quantidade, double MediaMinutos)> ObterTempoMedioExecucaoAsync()
        {
            try
            {
                return await _ordemServicoRepository.ObterTempoMedioExecucaoAsync();
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:ObterTempoMedioExecucaoAsync", _logger);
                throw;
            }
        }
        public async Task<IEnumerable<OrdemServico>> ObterPorClienteIdAsync(string clienteId)
        {
            try
            {
                if (!int.TryParse(clienteId, out var id))
                    throw new ArgumentException("ClienteId inválido.");

                return await _ordemServicoRepository.ObterPorClienteIdAsync(id);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:ObterPorClienteIdAsync", _logger);
                throw;
            }
        }

        private static string GetStatusDescription(StatusOrdemServico status) => status.Descricao();

        // OS criadas antes da coluna DataAlteracaoStatus usam a data de criação como
        // referência de entrada no status atual.
        private static DateTime InicioDoStatusAtual(OrdemServico ordemServico) =>
            ordemServico.DataAlteracaoStatus ?? ordemServico.DataCriacao;

        private void RegistrarTransicao(OrdemServico ordemServico, StatusOrdemServico statusAnterior,
                                        DateTime inicioStatusAnterior, DateTime agora)
        {
            // DataCriacao é gravada em horário local; num host fora de UTC a diferença
            // poderia sair negativa para OS antigas. Tempo negativo não tem significado.
            var tempo = agora - inicioStatusAnterior;

            _monitoramentoService.RegistrarMudancaStatus(ordemServico, statusAnterior,
                tempo < TimeSpan.Zero ? TimeSpan.Zero : tempo);
        }

        public async Task<OrdemServico?> ObterPorIdAsync(string id)
        {
            try
            {
                return await _ordemServicoRepository.ObterPorIdAsync(id);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:ObterPorIdAsync", _logger);
                throw;
            }
        }

        public async Task RemoverAsync(string id)
        {
            try
            {
                _ = await _ordemServicoRepository.ObterPorIdAsync(id)
                    ?? throw new KeyNotFoundException($"Ordem de Serviço com Id '{id}' não encontrada.");

                await _ordemServicoRepository.RemoverAsync(id);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:RemoverAsync", _logger);
                throw;
            }
        }

        public async Task<OrdemServico> ConfirmarAprovacaoPorTokenAsync(string token, bool aprovado, string? motivoRecusa)
        {
            try
            {
                var tokenEntity = await _ordemServicoAprovacaoTokenRepository.ObterPorTokenAsync(token)
                    ?? throw new KeyNotFoundException("Token de aprovação inválido.");

                if (tokenEntity.Utilizado)
                    throw new InvalidOperationException("Este token de aprovação já foi utilizado.");

                if (tokenEntity.DataExpiracao < DateTime.UtcNow)
                    throw new InvalidOperationException("Este token de aprovação expirou.");

                var ordemServico = aprovado
                    ? await AprovarAsync(tokenEntity.OrdemServicoId)
                    : await RecusarAsync(tokenEntity.OrdemServicoId, motivoRecusa ?? string.Empty);

                tokenEntity.Utilizado      = true;
                tokenEntity.DataUtilizacao = DateTime.UtcNow;
                await _ordemServicoAprovacaoTokenRepository.AtualizarAsync(tokenEntity);

                return ordemServico;
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:ConfirmarAprovacaoPorTokenAsync", _logger);
                throw;
            }
        }
    }
}

