using MechanicLtda.Web.ViewModels;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MechanicLtda.Web.Controllers
{
    public class OrdensServicoController : RazorControllerBase
    {
        private readonly IOrdemServicoAppService _ordemServicoAppService;
        private readonly IClienteAppService _clienteAppService;
        private readonly IVeiculoAppService _veiculoAppService;
        private readonly IItemOrdemServicoAppService _itemAppService;
        private readonly IOrcamentoAppService _orcamentoAppService;

        public OrdensServicoController(
            IOrdemServicoAppService ordemServicoAppService,
            IClienteAppService clienteAppService,
            IVeiculoAppService veiculoAppService,
            IItemOrdemServicoAppService itemAppService,
            IOrcamentoAppService orcamentoAppService)
        {
            _ordemServicoAppService = ordemServicoAppService;
            _clienteAppService = clienteAppService;
            _veiculoAppService = veiculoAppService;
            _itemAppService = itemAppService;
            _orcamentoAppService = orcamentoAppService;
        }

        public async Task<IActionResult> Index()
        {
            var ordens = await _ordemServicoAppService.ObterTodosAsync();
            var tempoMedio = await _ordemServicoAppService.ObterTempoMedioExecucaoAsync();
            FlashErrors(ordens);
            FlashErrors(tempoMedio);

            return View(new OrdensServicoIndexViewModel
            {
                Ordens = ordens.hasErrors ? Enumerable.Empty<OrdemServicoDto>() : ordens.getResponse,
                TempoMedioExecucao = tempoMedio.hasErrors ? new TempoMedioExecucaoDto() : tempoMedio.getResponse
            });
        }

        public async Task<IActionResult> Details(int id)
        {
            var ordem = await _ordemServicoAppService.ObterPorIdAsync(id.ToString());
            if (ordem.hasErrors)
            {
                FlashErrors(ordem);
                return RedirectToAction(nameof(Index));
            }

            var itens = await _itemAppService.ObterPorOrdemServicoIdAsync(id);
            FlashErrors(itens);

            var orcamento = await _orcamentoAppService.ObterPorOrdemServicoIdAsync(id);

            return View(new OrdemServicoDetalheViewModel
            {
                Ordem = ordem.getResponse,
                Itens = itens.hasErrors ? Enumerable.Empty<ItemOrdemServicoDto>() : itens.getResponse,
                Orcamento = orcamento.hasErrors ? null : orcamento.getResponse
            });
        }

        public async Task<IActionResult> Create()
        {
            var model = new OrdemServicoFormViewModel();
            await PopulateSelectionsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrdemServicoFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateSelectionsAsync(model);
                return View(model);
            }

            var response = await _ordemServicoAppService.AdicionarAsync(new OrdemServicoCreateDto
            {
                DescricaoProblema = model.DescricaoProblema,
                ValorTotalEstimado = model.ValorTotalEstimado,
                VeiculoId = model.VeiculoId,
                ClienteId = model.ClienteId
            });

            if (response.hasErrors)
            {
                AddErrors(response);
                await PopulateSelectionsAsync(model);
                return View(model);
            }

            FlashSuccess("Ordem de serviço aberta com sucesso.");
            return RedirectToAction(nameof(Details), new { id = response.getResponse.Id });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var response = await _ordemServicoAppService.ObterPorIdAsync(id.ToString());
            if (response.hasErrors)
            {
                FlashErrors(response);
                return RedirectToAction(nameof(Index));
            }

            var ordem = response.getResponse;
            var model = new OrdemServicoFormViewModel
            {
                Id = ordem.Id,
                DescricaoProblema = ordem.DescricaoProblema,
                ValorTotalEstimado = ordem.ValorTotalEstimado,
                VeiculoId = ordem.VeiculoId,
                ClienteId = ordem.ClienteId
            };

            await PopulateSelectionsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OrdemServicoFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateSelectionsAsync(model);
                return View(model);
            }

            var response = await _ordemServicoAppService.AtualizarAsync(id.ToString(), new OrdemServicoUpdateDto
            {
                DescricaoProblema = model.DescricaoProblema,
                ValorTotalEstimado = model.ValorTotalEstimado,
                VeiculoId = model.VeiculoId,
                ClienteId = model.ClienteId
            });

            if (response.hasErrors)
            {
                AddErrors(response);
                await PopulateSelectionsAsync(model);
                return View(model);
            }

            FlashSuccess("Ordem de serviço atualizada com sucesso.");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarStatus(int id, string acao)
        {
            var response = acao switch
            {
                "diagnostico" => await _ordemServicoAppService.IniciarDiagnosticoAsync(id.ToString()),
                "aprovacao" => await _ordemServicoAppService.AguardarAprovacaoAsync(id.ToString()),
                "execucao" => await _ordemServicoAppService.IniciarExecucaoAsync(id.ToString()),
                "finalizar" => await _ordemServicoAppService.FinalizarAsync(id.ToString()),
                "entregar" => await _ordemServicoAppService.EntregarAsync(id.ToString()),
                _ => new ResponseDto<OrdemServicoDto>().addError("Ação de status inválida.")
            };

            if (response.hasErrors)
                FlashErrors(response);
            else
                FlashSuccess("Status atualizado com sucesso.");

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _ordemServicoAppService.RemoverAsync(id.ToString());
            if (response.hasErrors)
                FlashErrors(response);
            else
                FlashSuccess("Ordem de serviço removida com sucesso.");

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateSelectionsAsync(OrdemServicoFormViewModel model)
        {
            var clientes = await _clienteAppService.ObterTodosAsync();
            var veiculos = await _veiculoAppService.ObterTodosAsync();
            FlashErrors(clientes);
            FlashErrors(veiculos);

            model.Clientes = clientes.hasErrors
                ? Enumerable.Empty<SelectListItem>()
                : clientes.getResponse
                    .Where(x => x.Ativo)
                    .OrderBy(x => x.Nome)
                    .Select(x => new SelectListItem(x.Nome, x.Id.ToString(), x.Id == model.ClienteId));

            model.Veiculos = veiculos.hasErrors
                ? Enumerable.Empty<SelectListItem>()
                : veiculos.getResponse
                    .Where(x => x.Ativo)
                    .OrderBy(x => x.Placa)
                    .Select(x => new SelectListItem($"{x.Placa} - {x.Marca} {x.Modelo}", x.Id.ToString(), x.Id == model.VeiculoId));
        }
    }
}


