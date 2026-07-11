using MechanicLtda.Web.ViewModels;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MechanicLtda.Web.Controllers
{
    public class EstoquesController : RazorControllerBase
    {
        private readonly IEstoqueAppService _estoqueAppService;

        public EstoquesController(IEstoqueAppService estoqueAppService)
        {
            _estoqueAppService = estoqueAppService;
        }

        public async Task<IActionResult> Index()
        {
            var response = await _estoqueAppService.ObterTodosAsync();
            FlashErrors(response);
            return View(response.hasErrors ? Enumerable.Empty<EstoqueDto>() : response.getResponse);
        }

        public IActionResult Create()
        {
            var model = new EstoqueFormViewModel();
            PopulateTipos(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EstoqueFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PopulateTipos(model);
                return View(model);
            }

            var response = await _estoqueAppService.AdicionarAsync(new EstoqueCreateDto
            {
                Nome = model.Nome,
                Tipo = model.Tipo,
                QuantidadeAtual = model.QuantidadeAtual,
                QuantidadeMinima = model.QuantidadeMinima
            });

            if (response.hasErrors)
            {
                AddErrors(response);
                PopulateTipos(model);
                return View(model);
            }

            FlashSuccess("Item de estoque cadastrado com sucesso.");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var response = await _estoqueAppService.ObterPorIdAsync(id.ToString());
            if (response.hasErrors)
            {
                FlashErrors(response);
                return RedirectToAction(nameof(Index));
            }

            var estoque = response.getResponse;
            var model = new EstoqueFormViewModel
            {
                Id = estoque.Id,
                Nome = estoque.Nome,
                Tipo = estoque.Tipo,
                QuantidadeAtual = estoque.QuantidadeAtual,
                QuantidadeMinima = estoque.QuantidadeMinima
            };

            PopulateTipos(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EstoqueFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PopulateTipos(model);
                return View(model);
            }

            var response = await _estoqueAppService.AtualizarAsync(id.ToString(), new EstoqueUpdateDto
            {
                Nome = model.Nome,
                Tipo = model.Tipo,
                QuantidadeMinima = model.QuantidadeMinima
            });

            if (response.hasErrors)
            {
                AddErrors(response);
                PopulateTipos(model);
                return View(model);
            }

            FlashSuccess("Estoque atualizado com sucesso.");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Repor(int id)
        {
            var response = await _estoqueAppService.ObterPorIdAsync(id.ToString());
            if (response.hasErrors)
            {
                FlashErrors(response);
                return RedirectToAction(nameof(Index));
            }

            return View(new EstoqueReposicaoRazorViewModel
            {
                Id = response.getResponse.Id,
                Nome = response.getResponse.Nome
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Repor(int id, EstoqueReposicaoRazorViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var response = await _estoqueAppService.ReporQuantidadeAsync(id.ToString(), new EstoqueReposicaoDto
            {
                QuantidadeEntrada = model.QuantidadeEntrada
            });

            if (response.hasErrors)
            {
                AddErrors(response);
                return View(model);
            }

            FlashSuccess("Quantidade reposta com sucesso.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _estoqueAppService.RemoverAsync(id.ToString());
            if (response.hasErrors)
                FlashErrors(response);
            else
                FlashSuccess("Item de estoque removido com sucesso.");

            return RedirectToAction(nameof(Index));
        }

        private static void PopulateTipos(EstoqueFormViewModel model)
        {
            model.Tipos = Enum.GetValues<TipoEstoque>()
                .Select(x => new SelectListItem(x.ToString(), ((int)x).ToString(), x == model.Tipo));
        }
    }
}


