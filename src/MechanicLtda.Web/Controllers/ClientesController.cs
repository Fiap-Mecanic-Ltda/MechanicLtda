using MechanicLtda.Web.ViewModels;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MechanicLtda.Web.Controllers
{
    public class ClientesController : RazorControllerBase
    {
        private readonly IClienteAppService _clienteAppService;

        public ClientesController(IClienteAppService clienteAppService)
        {
            _clienteAppService = clienteAppService;
        }

        public async Task<IActionResult> Index()
        {
            var response = await _clienteAppService.ObterTodosAsync();
            FlashErrors(response);
            return View(response.hasErrors ? Enumerable.Empty<ClienteDto>() : response.getResponse);
        }

        public IActionResult Create()
        {
            return View(new ClienteFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClienteFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var response = await _clienteAppService.AdicionarAsync(new ClienteCreateDto
            {
                Nome = model.Nome,
                Email = model.Email,
                Telefone = model.Telefone,
                CpfCnpj = model.CpfCnpj
            });

            if (response.hasErrors)
            {
                AddErrors(response);
                return View(model);
            }

            FlashSuccess("Cliente cadastrado com sucesso.");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var response = await _clienteAppService.ObterPorIdAsync(id.ToString());
            if (response.hasErrors)
            {
                FlashErrors(response);
                return RedirectToAction(nameof(Index));
            }

            var cliente = response.getResponse;
            return View(new ClienteFormViewModel
            {
                Id = cliente.Id,
                Nome = cliente.Nome,
                Email = cliente.Email,
                Telefone = cliente.Telefone,
                CpfCnpj = cliente.CpfCnpj,
                Ativo = cliente.Ativo
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ClienteFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var response = await _clienteAppService.AtualizarAsync(id.ToString(), new ClienteUpdateDto
            {
                Nome = model.Nome,
                Email = model.Email,
                Telefone = model.Telefone,
                CpfCnpj = model.CpfCnpj,
                Ativo = model.Ativo
            });

            if (response.hasErrors)
            {
                AddErrors(response);
                return View(model);
            }

            FlashSuccess("Cliente atualizado com sucesso.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _clienteAppService.RemoverAsync(id.ToString());
            if (response.hasErrors)
                FlashErrors(response);
            else
                FlashSuccess("Cliente removido com sucesso.");

            return RedirectToAction(nameof(Index));
        }
    }
}

