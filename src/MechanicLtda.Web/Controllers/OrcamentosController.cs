using MechanicLtda.Web.ViewModels;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MechanicLtda.Web.Controllers
{
    public class OrcamentosController : RazorControllerBase
    {
        private readonly IOrcamentoAppService _orcamentoAppService;

        public OrcamentosController(IOrcamentoAppService orcamentoAppService)
        {
            _orcamentoAppService = orcamentoAppService;
        }

        public async Task<IActionResult> Edit(int id)
        {
            var response = await _orcamentoAppService.ObterPorIdAsync(id.ToString());
            if (response.hasErrors)
            {
                FlashErrors(response);
                return RedirectToAction("Index", "OrdensServico");
            }

            var orcamento = response.getResponse;
            return View(new OrcamentoFormViewModel
            {
                Id = orcamento.Id,
                OrdemServicoId = orcamento.OrdemServicoId,
                ValorTotalPecas = orcamento.ValorTotalPecas,
                ValorTotalInsumos = orcamento.ValorTotalInsumos,
                ValorTotalGeral = orcamento.ValorTotalGeral,
                Validade = orcamento.Validade
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OrcamentoFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var response = await _orcamentoAppService.AtualizarManualAsync(id.ToString(), new OrcamentoUpdateDto
            {
                ValorTotalPecas = model.ValorTotalPecas,
                ValorTotalInsumos = model.ValorTotalInsumos,
                Validade = model.Validade
            });

            if (response.hasErrors)
            {
                AddErrors(response);
                return View(model);
            }

            FlashSuccess("Orçamento atualizado com sucesso.");
            return RedirectToAction("Details", "OrdensServico", new { id = model.OrdemServicoId });
        }

        public async Task<IActionResult> DownloadPdf(int id)
        {
            var response = await _orcamentoAppService.ExportarPdfAsync(id);
            if (response.hasErrors)
            {
                FlashErrors(response);
                return RedirectToAction("Index", "OrdensServico");
            }

            return File(response.getResponse, "application/pdf", $"orcamento-{id}.pdf");
        }
    }
}

