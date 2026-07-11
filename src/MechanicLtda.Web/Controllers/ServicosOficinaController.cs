using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace MechanicLtda.Web.Controllers
{
    public class ServicosOficinaController : RazorControllerBase
    {
        private readonly IServicoOficinaAppService _servicoAppService;

        public ServicosOficinaController(IServicoOficinaAppService servicoAppService)
        {
            _servicoAppService = servicoAppService;
        }

        public async Task<IActionResult> Index()
        {
            var response = await _servicoAppService.ObterTodosAsync();
            FlashErrors(response);
            return View(response.hasErrors ? Enumerable.Empty<ServicoOficinaDto>() : response.getResponse);
        }

        public IActionResult Create()
        {
            return View(new ServicoOficinaFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServicoOficinaFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var response = await _servicoAppService.AdicionarAsync(new ServicoOficinaCreateDto
            {
                Nome = model.Nome,
                Descricao = model.Descricao ?? string.Empty,
                ValorBase = model.ValorBase,
                Ativo = model.Ativo
            });

            if (response.hasErrors)
            {
                AddErrors(response);
                return View(model);
            }

            FlashSuccess("Servico cadastrado com sucesso.");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var response = await _servicoAppService.ObterPorIdAsync(id.ToString());
            if (response.hasErrors)
            {
                FlashErrors(response);
                return RedirectToAction(nameof(Index));
            }

            var servico = response.getResponse;
            return View(new ServicoOficinaFormViewModel
            {
                Id = servico.Id,
                Nome = servico.Nome,
                Descricao = servico.Descricao,
                ValorBase = servico.ValorBase,
                Ativo = servico.Ativo
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ServicoOficinaFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var response = await _servicoAppService.AtualizarAsync(id.ToString(), new ServicoOficinaUpdateDto
            {
                Nome = model.Nome,
                Descricao = model.Descricao ?? string.Empty,
                ValorBase = model.ValorBase,
                Ativo = model.Ativo
            });

            if (response.hasErrors)
            {
                AddErrors(response);
                return View(model);
            }

            FlashSuccess("Servico atualizado com sucesso.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _servicoAppService.RemoverAsync(id.ToString());
            if (response.hasErrors)
                FlashErrors(response);
            else
                FlashSuccess("Servico removido com sucesso.");

            return RedirectToAction(nameof(Index));
        }
    }
}