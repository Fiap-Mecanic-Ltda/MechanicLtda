using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Domain.Interfaces.Services;
using MechanicLtda.Domain.Services.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MechanicLtda.Domain.Services
{
    public class OrcamentoService : BaseService, IOrcamentoService
    {
        private readonly IOrcamentoRepository          _orcamentoRepository;
        private readonly IOrdemServicoRepository       _ordemServicoRepository;
        private readonly IItemOrdemServicoRepository   _itemRepository;
        private readonly IEstoqueRepository            _estoqueRepository;
        private readonly ILogger<OrcamentoService>     _logger;

        public OrcamentoService(
            ILogger<OrcamentoService>   logger,
            IConfiguration              configuration,
            INotificadorService         notificadorService,
            IOrcamentoRepository        orcamentoRepository,
            IOrdemServicoRepository     ordemServicoRepository,
            IItemOrdemServicoRepository itemRepository,
            IEstoqueRepository          estoqueRepository)
            : base(notificadorService, configuration)
        {
            _orcamentoRepository    = orcamentoRepository;
            _ordemServicoRepository = ordemServicoRepository;
            _itemRepository         = itemRepository;
            _estoqueRepository      = estoqueRepository;
            _logger                 = logger;
        }

        /// <summary>
        /// Cria ou atualiza o orçamento a partir dos itens atuais da OS.
        /// Chamado automaticamente sempre que itens são adicionados, modificados ou removidos.
        /// </summary>
        public async Task<Orcamento> CriarOuAtualizarAsync(int ordemServicoId)
        {
            try
            {
                _ = await _ordemServicoRepository.ObterPorIdAsync(ordemServicoId.ToString())
                    ?? throw new KeyNotFoundException($"Ordem de Serviço com Id '{ordemServicoId}' não encontrada.");

                var itens = (await _itemRepository.ObterPorOrdemServicoIdAsync(ordemServicoId)).ToList();

                var totalPecas    = 0m;
                var totalInsumos  = 0m;
                var totalGeral    = 0m;

                foreach (var item in itens)
                {
                    totalGeral += item.ValorTotal;

                    if (item.EstoqueId.HasValue)
                    {
                        var estoque = await _estoqueRepository.ObterPorIdAsync(item.EstoqueId.Value.ToString());
                        if (estoque is not null)
                        {
                            if (estoque.Tipo == TipoEstoque.Peca)
                                totalPecas += item.ValorTotal;
                            else if (estoque.Tipo == TipoEstoque.Insumo)
                                totalInsumos += item.ValorTotal;
                        }
                    }
                }

                var existente = await _orcamentoRepository.ObterPorOrdemServicoIdAsync(ordemServicoId);

                if (existente is null)
                {
                    var novo = new Orcamento
                    {
                        OrdemServicoId   = ordemServicoId,
                        ValorTotalPecas  = totalPecas,
                        ValorTotalInsumos = totalInsumos,
                        ValorTotalGeral  = totalGeral,
                        DataGeracao      = DateTime.UtcNow,
                        Validade         = DateTime.UtcNow.AddDays(30)
                    };

                    return await _orcamentoRepository.AdicionarAsync(novo);
                }

                existente.ValorTotalPecas   = totalPecas;
                existente.ValorTotalInsumos = totalInsumos;
                existente.ValorTotalGeral   = totalGeral;
                existente.DataGeracao       = DateTime.UtcNow;
                existente.Validade          = DateTime.UtcNow.AddDays(30);

                return await _orcamentoRepository.AtualizarAsync(existente);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo OrcamentoService:CriarOuAtualizarAsync", _logger);
                throw;
            }
        }

        /// <summary>
        /// Permite ajustes manuais de valores sobre um orçamento existente.
        /// </summary>
        public async Task<Orcamento> AtualizarManualAsync(Orcamento orcamento)
        {
            try
            {
                var existente = await _orcamentoRepository.ObterPorIdAsync(orcamento.Id.ToString())
                    ?? throw new KeyNotFoundException($"Orçamento com Id '{orcamento.Id}' não encontrado.");

                existente.ValorTotalPecas   = orcamento.ValorTotalPecas;
                existente.ValorTotalInsumos = orcamento.ValorTotalInsumos;
                existente.ValorTotalGeral   = orcamento.ValorTotalPecas + orcamento.ValorTotalInsumos;
                existente.Validade          = orcamento.Validade;
                existente.DataGeracao       = DateTime.UtcNow;

                return await _orcamentoRepository.AtualizarAsync(existente);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo OrcamentoService:AtualizarManualAsync", _logger);
                throw;
            }
        }

        public async Task<Orcamento?> ObterPorIdAsync(string id)
        {
            try
            {
                return await _orcamentoRepository.ObterPorIdAsync(id);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo OrcamentoService:ObterPorIdAsync", _logger);
                throw;
            }
        }

        public async Task<Orcamento?> ObterPorOrdemServicoIdAsync(int ordemServicoId)
        {
            try
            {
                return await _orcamentoRepository.ObterPorOrdemServicoIdAsync(ordemServicoId);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo OrcamentoService:ObterPorOrdemServicoIdAsync", _logger);
                throw;
            }
        }

        public async Task RemoverAsync(string id)
        {
            try
            {
                _ = await _orcamentoRepository.ObterPorIdAsync(id)
                    ?? throw new KeyNotFoundException($"Orçamento com Id '{id}' não encontrado.");

                await _orcamentoRepository.RemoverAsync(id);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo OrcamentoService:RemoverAsync", _logger);
                throw;
            }
        }

        public async Task<Orcamento?> ObterComDetalhesAsync(int id)
        {
            return await _orcamentoRepository.ObterComDetalhesAsync(id);
        }
    }
}