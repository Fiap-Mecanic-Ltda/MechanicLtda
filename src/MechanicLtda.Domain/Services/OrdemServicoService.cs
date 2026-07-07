using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Domain.Interfaces.Services;
using MechanicLtda.Domain.Services.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MechanicLtda.Domain.Services
{
    public class OrdemServicoService : BaseService, IOrdemServicoService
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly IVeiculoRepository _veiculoRepository;
        private readonly ILogger<OrdemServicoService> _logger;

        public OrdemServicoService(ILogger<OrdemServicoService> logger,
                                   IConfiguration configuration,
                                   INotificadorService notificadorService,
                                   IOrdemServicoRepository ordemServicoRepository,
                                   IVeiculoRepository veiculoRepository) : base(notificadorService, configuration)
        {
            _ordemServicoRepository = ordemServicoRepository;
            _veiculoRepository = veiculoRepository;
            _logger = logger;
        }

        public async Task<OrdemServico> AdicionarAsync(string descricaoProblema, decimal? valorTotalEstimado, int veiculoId, int clienteId)
        {
            try
            {
                // Converter string para int para buscar o veículo
                if (!int.TryParse(veiculoId.ToString(), out var veiculoIdInt))
                    throw new ArgumentException($"VeiculoId inválido: {veiculoId}");

                var veiculo = await _veiculoRepository.ObterPorIdAsync(veiculoIdInt.ToString())
                    ?? throw new KeyNotFoundException($"Veículo com Id '{veiculoId}' não encontrado.");

                if (veiculo.ClienteId != clienteId)
                    throw new InvalidOperationException("O veículo informado não pertence ao cliente indicado.");

                var ordemServico = new OrdemServico
                {
                    DescricaoProblema  = descricaoProblema,
                    ValorTotalEstimado = valorTotalEstimado,
                    VeiculoId          = veiculoId,
                    ClienteId          = clienteId,
                    Status             = StatusOrdemServico.Recebida,
                    DataCriacao        = DateTime.Now
                };

                return await _ordemServicoRepository.AdicionarAsync(ordemServico);
            }
            catch (Exception ex)
            {
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

                ordemServico.Status          = existente.Status;
                ordemServico.DataCriacao     = existente.DataCriacao;
                ordemServico.DataModificacao = DateTime.UtcNow;

                // Gatilho: ao preencher descrição + valor estimado na OS recebida, avança para Em Diagnóstico
                if (!string.IsNullOrWhiteSpace(ordemServico.DescricaoProblema) &&
                    ordemServico.ValorTotalEstimado.HasValue &&
                    existente.Status == StatusOrdemServico.Recebida)
                {
                    ordemServico.Status = StatusOrdemServico.EmDiagnostico;
                }

                return await _ordemServicoRepository.AtualizarAsync(ordemServico);
            }
            catch (Exception ex)
            {
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

                ordemServico.Status          = StatusOrdemServico.EmDiagnostico;
                ordemServico.DataModificacao = DateTime.UtcNow;

                return await _ordemServicoRepository.AtualizarAsync(ordemServico);
            }
            catch (Exception ex)
            {
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

                ordemServico.Status          = StatusOrdemServico.AguardandoAprovacao;
                ordemServico.DataModificacao = DateTime.UtcNow;

                return await _ordemServicoRepository.AtualizarAsync(ordemServico);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:AguardarAprovacaoAsync", _logger);
                throw;
            }
        }

        public async Task<OrdemServico> AprovarAsync(int id)
        {
            try
            {
                var ordemServico = await _ordemServicoRepository.ObterPorIdAsync(id.ToString())
                    ?? throw new KeyNotFoundException($"Ordem de Serviço com Id '{id}' não encontrada.");

                if (ordemServico.Status != StatusOrdemServico.AguardandoAprovacao)
                    throw new InvalidOperationException($"A OS só pode ser aprovada quando estiver 'Aguardando Aprovação'. Status atual: {ordemServico.Status}.");

                ordemServico.Status          = StatusOrdemServico.EmExecucao;
                ordemServico.DataModificacao = DateTime.UtcNow;

                return await _ordemServicoRepository.AtualizarAsync(ordemServico);
            }
            catch (Exception ex)
            {
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

                // Voltar para Em Diagnóstico para revisão
                ordemServico.Status          = StatusOrdemServico.EmDiagnostico;
                ordemServico.DataModificacao = DateTime.UtcNow;

                // Adicionar motivo da recusa na descrição ou em outro campo se disponível
                if (!string.IsNullOrWhiteSpace(motivoRecusa))
                    ordemServico.DescricaoProblema = $"{ordemServico.DescricaoProblema}\n[RECUSA]: {motivoRecusa}";

                return await _ordemServicoRepository.AtualizarAsync(ordemServico);
            }
            catch (Exception ex)
            {
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

                ordemServico.Status          = StatusOrdemServico.EmExecucao;
                ordemServico.DataModificacao = DateTime.UtcNow;

                return await _ordemServicoRepository.AtualizarAsync(ordemServico);
            }
            catch (Exception ex)
            {
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

                ordemServico.Status          = StatusOrdemServico.Finalizada;
                ordemServico.DataModificacao = DateTime.UtcNow;

                return await _ordemServicoRepository.AtualizarAsync(ordemServico);
            }
            catch (Exception ex)
            {
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

                ordemServico.Status          = StatusOrdemServico.Entregue;
                ordemServico.DataModificacao = DateTime.UtcNow;

                return await _ordemServicoRepository.AtualizarAsync(ordemServico);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:EntregarAsync", _logger);
                throw;
            }
        }

        public async Task<IEnumerable<OrdemServico>> ObterTodosAsync()
        {
            try
            {
                return await _ordemServicoRepository.ObterTodosAsync();
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:ObterTodosAsync", _logger);
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

        public async Task<IEnumerable<OrdemServico>> ObterPorStatusAsync(string statusDescricao)
        {
            try
            {
                // Obter todas as ordens de serviço
                var todasAsOrdens = await _ordemServicoRepository.ObterTodosAsync();

                // Filtrar por StatusDescricao usando Display
                var ordensFiltradas = todasAsOrdens.Where(os => GetStatusDescription(os.Status) == statusDescricao);

                return ordensFiltradas;
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:ObterPorStatusAsync", _logger);
                throw;
            }
        }

        private static string GetStatusDescription(StatusOrdemServico status)
        {
            var type = status.GetType();
            var name = Enum.GetName(type, status);
            if (name == null) return status.ToString();

            var field = type.GetField(name);
            if (field == null) return status.ToString();

            var displayAttribute = field.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false)
                .FirstOrDefault() as System.ComponentModel.DataAnnotations.DisplayAttribute;

            return displayAttribute?.Name ?? status.ToString();
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
    }
}