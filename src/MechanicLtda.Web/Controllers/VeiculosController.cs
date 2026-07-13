using MechanicLtda.Web.ViewModels;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MechanicLtda.Web.Controllers
{
    public class VeiculosController : RazorControllerBase
    {
        private readonly IVeiculoAppService _veiculoAppService;
        private readonly IClienteAppService _clienteAppService;

        public VeiculosController(IVeiculoAppService veiculoAppService, IClienteAppService clienteAppService)
        {
            _veiculoAppService = veiculoAppService;
            _clienteAppService = clienteAppService;
        }

        public async Task<IActionResult> Index()
        {
            var response = await _veiculoAppService.ObterTodosAsync();
            FlashErrors(response);
            return View(response.hasErrors ? Enumerable.Empty<VeiculoDto>() : response.getResponse);
        }

        public async Task<IActionResult> Create()
        {
            var model = new VeiculoFormViewModel();
            await PopulateClientesAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VeiculoFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateClientesAsync(model);
                return View(model);
            }

            var response = await _veiculoAppService.AdicionarAsync(new VeiculoCreateDto
            {
                Placa = model.Placa,
                Marca = model.Marca,
                Modelo = model.Modelo,
                Ano = model.Ano,
                ClienteId = model.ClienteId
            });

            if (response.hasErrors)
            {
                AddErrors(response);
                await PopulateClientesAsync(model);
                return View(model);
            }

            FlashSuccess("Veículo cadastrado com sucesso.");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var response = await _veiculoAppService.ObterPorIdAsync(id.ToString());
            if (response.hasErrors)
            {
                FlashErrors(response);
                return RedirectToAction(nameof(Index));
            }

            var veiculo = response.getResponse;
            var model = new VeiculoFormViewModel
            {
                Id = veiculo.Id,
                Placa = veiculo.Placa,
                Marca = veiculo.Marca,
                Modelo = veiculo.Modelo,
                Ano = veiculo.Ano,
                Ativo = veiculo.Ativo,
                ClienteId = veiculo.ClienteId
            };

            await PopulateClientesAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VeiculoFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateClientesAsync(model);
                return View(model);
            }

            var response = await _veiculoAppService.AtualizarAsync(id.ToString(), new VeiculoUpdateDto
            {
                Placa = model.Placa,
                Marca = model.Marca,
                Modelo = model.Modelo,
                Ano = model.Ano,
                Ativo = model.Ativo,
                ClienteId = model.ClienteId
            });

            if (response.hasErrors)
            {
                AddErrors(response);
                await PopulateClientesAsync(model);
                return View(model);
            }

            FlashSuccess("Veículo atualizado com sucesso.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _veiculoAppService.RemoverAsync(id.ToString());
            if (response.hasErrors)
                FlashErrors(response);
            else
                FlashSuccess("Veículo removido com sucesso.");

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateClientesAsync(VeiculoFormViewModel model)
        {
            var clientes = await _clienteAppService.ObterTodosAsync();
            FlashErrors(clientes);
            model.Clientes = clientes.hasErrors
                ? Enumerable.Empty<SelectListItem>()
                : clientes.getResponse
                    .Where(x => x.Ativo)
                    .OrderBy(x => x.Nome)
                    .Select(x => new SelectListItem(x.Nome, x.Id.ToString(), x.Id == model.ClienteId));
        }
    }
}


