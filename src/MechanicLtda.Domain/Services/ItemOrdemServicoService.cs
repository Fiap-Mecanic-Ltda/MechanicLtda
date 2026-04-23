using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Domain.Interfaces.Services;
using MechanicLtda.Domain.Services.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MechanicLtda.Domain.Services
{
    public class ItemOrdemServicoService : BaseService, IItemOrdemServicoService
    {
        private readonly IItemOrdemServicoRepository _itemRepository;
        private readonly IOrdemServicoRepository     _ordemServicoRepository;
        private readonly IEstoqueService             _estoqueService;
        private readonly ILogger<ItemOrdemServicoService> _logger;

        public ItemOrdemServicoService(
            ILogger<ItemOrdemServicoService> logger,
            IConfiguration configuration,
            INotificadorService notificadorService,
            IItemOrdemServicoRepository itemRepository,
            IOrdemServicoRepository ordemServicoRepository,
            IEstoqueService estoqueService) : base(notificadorService, configuration)
        {
            _logger                 = logger;
            _itemRepository         = itemRepository;
            _ordemServicoRepository = ordemServicoRepository;
            _estoqueService         = estoqueService;
        }

        public async Task<ItemOrdemServico> AdicionarAsync(
            int ordemServicoId, int? estoqueId, int quantidade, decimal valorUnitario)
        {
            try
            {
                _ = await _ordemServicoRepository.ObterPorIdAsync(ordemServicoId.ToString())
                    ?? throw new KeyNotFoundException(
                        $"Ordem de Serviço com Id '{ordemServicoId}' não encontrada.");

                // Realiza a baixa no estoque quando um EstoqueId for informado
                if (estoqueId.HasValue)
                    await _estoqueService.SubtrairQuantidadeAsync(estoqueId.Value, quantidade);

                var item = new ItemOrdemServico
                {
                    OrdemServicoId = ordemServicoId,
                    EstoqueId      = estoqueId,
                    Quantidade     = quantidade,
                    ValorUnitario  = valorUnitario
                };

                item.CalcularValorTotal();

                var itemCriado = await _itemRepository.AdicionarAsync(item);

                await AtualizarValorTotalOrdemServicoAsync(ordemServicoId);

                return itemCriado;
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método ItemOrdemServicoService:AdicionarAsync", _logger);
                throw;
            }
        }

        public async Task<ItemOrdemServico> AtualizarAsync(ItemOrdemServico item)
        {
            try
            {
                var existente = await _itemRepository.ObterPorIdAsync(item.Id.ToString())
                    ?? throw new KeyNotFoundException($"Item com Id '{item.Id}' não encontrado.");

                item.OrdemServicoId = existente.OrdemServicoId;
                item.CalcularValorTotal();

                var itemAtualizado = await _itemRepository.AtualizarAsync(item);

                await AtualizarValorTotalOrdemServicoAsync(item.OrdemServicoId);

                return itemAtualizado;
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método ItemOrdemServicoService:AtualizarAsync", _logger);
                throw;
            }
        }

        public async Task RemoverAsync(string id)
        {
            try
            {
                var existente = await _itemRepository.ObterPorIdAsync(id)
                    ?? throw new KeyNotFoundException($"Item com Id '{id}' não encontrado.");

                var ordemServicoId = existente.OrdemServicoId;

                await _itemRepository.RemoverAsync(id);

                await AtualizarValorTotalOrdemServicoAsync(ordemServicoId);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método ItemOrdemServicoService:RemoverAsync", _logger);
                throw;
            }
        }

        public async Task<IEnumerable<ItemOrdemServico>> ObterPorOrdemServicoIdAsync(int ordemServicoId)
        {
            try
            {
                return await _itemRepository.ObterPorOrdemServicoIdAsync(ordemServicoId);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método ItemOrdemServicoService:ObterPorOrdemServicoIdAsync", _logger);
                throw;
            }
        }

        public async Task<ItemOrdemServico?> ObterPorIdAsync(string id)
        {
            try
            {
                return await _itemRepository.ObterPorIdAsync(id);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no método ItemOrdemServicoService:ObterPorIdAsync", _logger);
                throw;
            }
        }

        // Trigger: recalcula e persiste o ValorTotalEstimado da OS após qualquer alteração nos itens.
        // Quando a entidade Orcamento for implementada, este método também deverá atualizá-la.
        private async Task AtualizarValorTotalOrdemServicoAsync(int ordemServicoId)
        {
            var ordemServico = await _ordemServicoRepository.ObterPorIdAsync(ordemServicoId.ToString());
            if (ordemServico is null) return;

            var itens = await _itemRepository.ObterPorOrdemServicoIdAsync(ordemServicoId);
            ordemServico.ValorTotalEstimado = itens.Sum(i => i.ValorTotal);
            ordemServico.DataModificacao    = DateTime.UtcNow;

            await _ordemServicoRepository.AtualizarAsync(ordemServico);
        }
    }
}