using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Domain.Interfaces.Services;
using MechanicLtda.Domain.Services.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MechanicLtda.Domain.Services
{
    public class EstoqueService : BaseService, IEstoqueService
    {
        private readonly IEstoqueRepository _estoqueRepository;
        private readonly ILogger<EstoqueService> _logger;

        public EstoqueService(
            ILogger<EstoqueService> logger,
            IConfiguration configuration,
            INotificadorService notificadorService,
            IEstoqueRepository estoqueRepository) : base(notificadorService, configuration)
        {
            _logger = logger;
            _estoqueRepository = estoqueRepository;
        }

        public async Task<Estoque> AdicionarAsync(Estoque estoque)
        {
            try
            {
                estoque.DataUltimaAtualizacao = DateTime.UtcNow;
                return await _estoqueRepository.AdicionarAsync(estoque);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método EstoqueService:AdicionarAsync", _logger);
                throw;
            }
        }

        public async Task<Estoque> AtualizarAsync(Estoque estoque)
        {
            try
            {
                var existente = await _estoqueRepository.ObterPorIdAsync(estoque.Id.ToString())
                    ?? throw new KeyNotFoundException($"Estoque com Id '{estoque.Id}' não encontrado.");

                existente.Nome                 = estoque.Nome;
                existente.Tipo                 = estoque.Tipo;
                existente.QuantidadeMinima     = estoque.QuantidadeMinima;
                existente.DataUltimaAtualizacao = DateTime.UtcNow;

                return await _estoqueRepository.AtualizarAsync(existente);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método EstoqueService:AtualizarAsync", _logger);
                throw;
            }
        }

        public async Task RemoverAsync(string id)
        {
            try
            {
                _ = await _estoqueRepository.ObterPorIdAsync(id)
                    ?? throw new KeyNotFoundException($"Estoque com Id '{id}' não encontrado.");

                await _estoqueRepository.RemoverAsync(id);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método EstoqueService:RemoverAsync", _logger);
                throw;
            }
        }

        public async Task<IEnumerable<Estoque>> ObterTodosAsync()
        {
            try
            {
                return await _estoqueRepository.ObterTodosAsync();
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método EstoqueService:ObterTodosAsync", _logger);
                throw;
            }
        }

        public async Task<IEnumerable<Estoque>> ObterPorTipoAsync(TipoEstoque tipo)
        {
            try
            {
                return await _estoqueRepository.ObterPorTipoAsync(tipo);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método EstoqueService:ObterPorTipoAsync", _logger);
                throw;
            }
        }

        public async Task<Estoque?> ObterPorIdAsync(string id)
        {
            try
            {
                return await _estoqueRepository.ObterPorIdAsync(id);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método EstoqueService:ObterPorIdAsync", _logger);
                throw;
            }
        }

        public async Task<Estoque> SubtrairQuantidadeAsync(int estoqueId, int quantidade)
        {
            try
            {
                var estoque = await _estoqueRepository.ObterPorIdAsync(estoqueId.ToString());

                // Regra: se não existir, cria com Quantidade = 0
                if (estoque is null)
                {
                    _logger.LogWarning(
                        "Estoque com Id '{EstoqueId}' não encontrado. Criando registro automático com Quantidade = 0.",
                        estoqueId);

                    Notificar($"[ALERTA] Estoque Id '{estoqueId}' não encontrado. Registro criado automaticamente com saldo zero.");

                    estoque = new Estoque
                    {
                        Id                   = estoqueId,
                        Nome                 = $"Item #{estoqueId} (criado automaticamente)",
                        Tipo                 = TipoEstoque.Peca,
                        QuantidadeAtual      = 0,
                        QuantidadeMinima     = 0,
                        DataUltimaAtualizacao = DateTime.UtcNow
                    };
                    return await _estoqueRepository.AdicionarAsync(estoque);
                }

                if (estoque.QuantidadeAtual < quantidade)
                    throw new InvalidOperationException(
                        $"[show]Saldo insuficiente no estoque '{estoque.Nome}'. " +
                        $"Disponível: {estoque.QuantidadeAtual}, Solicitado: {quantidade}.");

                estoque.QuantidadeAtual      -= quantidade;
                estoque.DataUltimaAtualizacao = DateTime.UtcNow;

                var estoqueAtualizado = await _estoqueRepository.AtualizarAsync(estoque);

                // Regra: notificação de baixo estoque ou falta de itens
                if (estoqueAtualizado.EstaBaixoEstoque())
                {
                    var mensagem = estoqueAtualizado.QuantidadeAtual == 0
                        ? $"[ALERTA CRÍTICO] Item '{estoqueAtualizado.Nome}' está sem estoque."
                        : $"[ALERTA] Item '{estoqueAtualizado.Nome}' atingiu nível mínimo " +
                          $"({estoqueAtualizado.QuantidadeAtual}/{estoqueAtualizado.QuantidadeMinima}).";

                    _logger.LogWarning(mensagem);
                    Notificar(mensagem);
                }

                return estoqueAtualizado;
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método EstoqueService:SubtrairQuantidadeAsync", _logger);
                throw;
            }
        }

        public async Task<Estoque> ReporQuantidadeAsync(int estoqueId, int quantidadeEntrada)
        {
            try
            {
                if (quantidadeEntrada <= 0)
                    throw new ArgumentException("[show]A quantidade de entrada deve ser maior que zero.");

                var estoque = await _estoqueRepository.ObterPorIdAsync(estoqueId.ToString())
                    ?? throw new KeyNotFoundException($"Estoque com Id '{estoqueId}' não encontrado.");

                estoque.QuantidadeAtual      += quantidadeEntrada;
                estoque.DataUltimaAtualizacao = DateTime.UtcNow;

                return await _estoqueRepository.AtualizarAsync(estoque);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método EstoqueService:ReporQuantidadeAsync", _logger);
                throw;
            }
        }
    }
}