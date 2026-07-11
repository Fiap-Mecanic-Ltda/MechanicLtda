using MechanicLtda.Web.ViewModels;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace MechanicLtda.Web.Controllers
{
    public class HomeController : RazorControllerBase
    {
        private readonly IClienteAppService _clienteAppService;
        private readonly IVeiculoAppService _veiculoAppService;
        private readonly IOrdemServicoAppService _ordemServicoAppService;
        private readonly IEstoqueAppService _estoqueAppService;

        public HomeController(
            IClienteAppService clienteAppService,
            IVeiculoAppService veiculoAppService,
            IOrdemServicoAppService ordemServicoAppService,
            IEstoqueAppService estoqueAppService)
        {
            _clienteAppService = clienteAppService;
            _veiculoAppService = veiculoAppService;
            _ordemServicoAppService = ordemServicoAppService;
            _estoqueAppService = estoqueAppService;
        }

        public async Task<IActionResult> Index()
        {
            var clientes = await _clienteAppService.ObterTodosAsync();
            var veiculos = await _veiculoAppService.ObterTodosAsync();
            var ordens = await _ordemServicoAppService.ObterTodosAsync();
            var estoques = await _estoqueAppService.ObterTodosAsync();

            FlashErrors(clientes);
            FlashErrors(veiculos);
            FlashErrors(ordens);
            FlashErrors(estoques);

            var ordensLista = ordens.hasErrors ? new List<OrdemServicoDto>() : ordens.getResponse.ToList();
            var estoquesLista = estoques.hasErrors ? new List<EstoqueDto>() : estoques.getResponse.ToList();

            var model = new DashboardViewModel
            {
                TotalClientes = clientes.hasErrors ? 0 : clientes.getResponse.Count(),
                TotalVeiculos = veiculos.hasErrors ? 0 : veiculos.getResponse.Count(),
                TotalOrdensAbertas = ordensLista.Count(x => x.Status != StatusOrdemServico.Entregue),
                TotalItensBaixoEstoque = estoquesLista.Count(x => x.BaixoEstoque),
                OrdensRecentes = ordensLista.OrderByDescending(x => x.DataCriacao).Take(5),
                EstoquesCriticos = estoquesLista.Where(x => x.BaixoEstoque).OrderBy(x => x.QuantidadeAtual).Take(5)
            };

            return View(model);
        }
    }
}


