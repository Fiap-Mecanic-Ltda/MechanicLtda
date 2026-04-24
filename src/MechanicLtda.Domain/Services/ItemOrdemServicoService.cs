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
        private readonly IOrcamentoService           _orcamentoService;
        private readonly ILogger<ItemOrdemServicoService> _logger;

        public ItemOrdemServicoService(
            ILogger<ItemOrdemServicoService> logger,
            IConfiguration                  configuration,
            INotificadorService             notificadorService,
            IItemOrdemServicoRepository     itemRepository,
            IOrdemServicoRepository         ordemServicoRepository,
            IEstoqueService                 estoqueService,
            IOrcamentoService               orcamentoService)
            : base(notificadorService, configuration)
        {
            _itemRepository         = itemRepository;
            _ordemServicoRepository = ordemServicoRepository;
            _estoqueService         = estoqueService;
            _orcamentoService       = orcamentoService;
            _logger                 = logger;
        }

        public async Task<ItemOrdemServico> AdicionarAsync(
            int ordemServicoId, int? estoqueId, int quantidade, decimal valorUnitario)
        {
            try
            {
                var os = await _ordemServicoRepository.ObterPorIdAsync(ordemServicoId.ToString())
                    ?? throw new KeyNotFoundException($"Ordem de Serviço com Id '{ordemServicoId}' não encontrada.");

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

                var resultado = await _itemRepository.AdicionarAsync(item);

                await AtualizarTotaisAsync(os);
                await _orcamentoService.CriarOuAtualizarAsync(ordemServicoId);

                return resultado;
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo ItemOrdemServicoService:AdicionarAsync", _logger);
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

                var resultado = await _itemRepository.AtualizarAsync(item);

                var os = await _ordemServicoRepository.ObterPorIdAsync(existente.OrdemServicoId.ToString());
                if (os is not null)
                {
                    await AtualizarTotaisAsync(os);
                    await _orcamentoService.CriarOuAtualizarAsync(os.Id);
                }

                return resultado;
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo ItemOrdemServicoService:AtualizarAsync", _logger);
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

                var os = await _ordemServicoRepository.ObterPorIdAsync(ordemServicoId.ToString());
                if (os is not null)
                {
                    await AtualizarTotaisAsync(os);
                    await _orcamentoService.CriarOuAtualizarAsync(ordemServicoId);
                }
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo ItemOrdemServicoService:RemoverAsync", _logger);
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
                Notificar(ex, "Ocorreu um erro no metodo ItemOrdemServicoService:ObterPorOrdemServicoIdAsync", _logger);
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
                Notificar(ex, "Ocorreu um erro no metodo ItemOrdemServicoService:ObterPorIdAsync", _logger);
                throw;
            }
        }

        // ─── Privado ────────────────────────────────────────────────────────────

        private async Task AtualizarTotaisAsync(OrdemServico os)
        {
            var itens = await _itemRepository.ObterPorOrdemServicoIdAsync(os.Id);
            os.ValorTotalEstimado = itens.Sum(i => i.ValorTotal);
            os.DataModificacao    = DateTime.UtcNow;
            await _ordemServicoRepository.AtualizarAsync(os);
        }
    }
}