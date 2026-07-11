using MechanicLtda.Web.ViewModels;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MechanicLtda.Web.Controllers
{
    public class ItensOrdemServicoController : RazorControllerBase
    {
        private readonly IItemOrdemServicoAppService _itemAppService;
        private readonly IEstoqueAppService _estoqueAppService;
        private readonly IServicoOficinaAppService _servicoAppService;

        public ItensOrdemServicoController(
            IItemOrdemServicoAppService itemAppService,
            IEstoqueAppService estoqueAppService,
            IServicoOficinaAppService servicoAppService)
        {
            _itemAppService = itemAppService;
            _estoqueAppService = estoqueAppService;
            _servicoAppService = servicoAppService;
        }

        public async Task<IActionResult> Create(int ordemServicoId)
        {
            var model = new ItemOrdemServicoFormViewModel { OrdemServicoId = ordemServicoId };
            await PopulateSelectionsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ItemOrdemServicoFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateSelectionsAsync(model);
                return View(model);
            }

            var response = await _itemAppService.AdicionarAsync(model.OrdemServicoId, new ItemOrdemServicoCreateDto
            {
                EstoqueId = model.EstoqueId,
                ServicoOficinaId = model.ServicoOficinaId,
                DescricaoServico = model.DescricaoServico,
                Quantidade = model.Quantidade,
                ValorUnitario = model.ValorUnitario
            });

            if (response.hasErrors)
            {
                AddErrors(response);
                await PopulateSelectionsAsync(model);
                return View(model);
            }

            FlashSuccess("Item adicionado a ordem de servico.");
            return RedirectToAction("Details", "OrdensServico", new { id = model.OrdemServicoId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var response = await _itemAppService.ObterPorIdAsync(id.ToString());
            if (response.hasErrors)
            {
                FlashErrors(response);
                return RedirectToAction("Index", "OrdensServico");
            }

            var item = response.getResponse;
            var model = new ItemOrdemServicoFormViewModel
            {
                Id = item.Id,
                OrdemServicoId = item.OrdemServicoId,
                EstoqueId = item.EstoqueId,
                ServicoOficinaId = item.ServicoOficinaId,
                DescricaoServico = item.DescricaoServico,
                Quantidade = item.Quantidade,
                ValorUnitario = item.ValorUnitario
            };

            await PopulateSelectionsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ItemOrdemServicoFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateSelectionsAsync(model);
                return View(model);
            }

            var response = await _itemAppService.AtualizarAsync(id.ToString(), new ItemOrdemServicoUpdateDto
            {
                EstoqueId = model.EstoqueId,
                ServicoOficinaId = model.ServicoOficinaId,
                DescricaoServico = model.DescricaoServico,
                Quantidade = model.Quantidade,
                ValorUnitario = model.ValorUnitario
            });

            if (response.hasErrors)
            {
                AddErrors(response);
                await PopulateSelectionsAsync(model);
                return View(model);
            }

            FlashSuccess("Item atualizado com sucesso.");
            return RedirectToAction("Details", "OrdensServico", new { id = model.OrdemServicoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int ordemServicoId)
        {
            var response = await _itemAppService.RemoverAsync(id.ToString());
            if (response.hasErrors)
                FlashErrors(response);
            else
                FlashSuccess("Item removido da ordem de servico.");

            return RedirectToAction("Details", "OrdensServico", new { id = ordemServicoId });
        }

        private async Task PopulateSelectionsAsync(ItemOrdemServicoFormViewModel model)
        {
            var estoques = await _estoqueAppService.ObterTodosAsync();
            var servicos = await _servicoAppService.ObterAtivosAsync();
            FlashErrors(estoques);
            FlashErrors(servicos);

            model.Estoques = estoques.hasErrors
                ? Enumerable.Empty<SelectListItem>()
                : estoques.getResponse
                    .OrderBy(x => x.Nome)
                    .Select(x => new SelectListItem($"{x.Nome} ({x.Tipo})", x.Id.ToString(), x.Id == model.EstoqueId));

            model.ServicosOficina = servicos.hasErrors
                ? Enumerable.Empty<SelectListItem>()
                : servicos.getResponse
                    .OrderBy(x => x.Nome)
                    .Select(x => new SelectListItem($"{x.Nome} ({x.ValorBase:C})", x.Id.ToString(), x.Id == model.ServicoOficinaId));
        }
    }
}
