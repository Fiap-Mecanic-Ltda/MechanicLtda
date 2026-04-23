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
                    Status             = StatusOrdemServico.EmAberto,
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

                ordemServico.Status      = existente.Status;
                ordemServico.DataCriacao = existente.DataCriacao;
                ordemServico.DataModificacao = DateTime.UtcNow;

                // Gatilho: ao atualizar com dados preenchidos, avança para EmValidacao
                if (!string.IsNullOrWhiteSpace(ordemServico.DescricaoProblema) &&
                    ordemServico.ValorTotalEstimado.HasValue &&
                    existente.Status == StatusOrdemServico.EmAberto)
                {
                    ordemServico.Status = StatusOrdemServico.EmValidacao;
                }

                return await _ordemServicoRepository.AtualizarAsync(ordemServico);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:AtualizarAsync", _logger);
                throw;
            }
        }

        public async Task<OrdemServico> MoverParaEmValidacaoAsync(int id)
        {
            try
            {
                var ordemServico = await _ordemServicoRepository.ObterPorIdAsync(id.ToString())
                    ?? throw new KeyNotFoundException($"Ordem de Serviço com Id '{id}' não encontrada.");

                if (ordemServico.Status != StatusOrdemServico.EmAberto)
                    throw new InvalidOperationException($"A OS só pode ser movida para 'Em Validação' quando estiver 'Em Aberto'. Status atual: {ordemServico.Status}.");

                ordemServico.Status          = StatusOrdemServico.EmValidacao;
                ordemServico.DataModificacao = DateTime.UtcNow;

                return await _ordemServicoRepository.AtualizarAsync(ordemServico);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método OrdemServicoService:MoverParaEmValidacaoAsync", _logger);
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