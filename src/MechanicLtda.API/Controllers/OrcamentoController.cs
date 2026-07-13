using AutoMapper;
using MechanicLtda.Bootstrap.Authorization;
using MechanicLtda.API.ViewModels;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicLtda.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.Admin)]
    public class OrcamentoController : ControllerBase
    {
        private readonly IOrcamentoAppService _orcamentoAppService;
        private readonly IMapper              _mapper;

        public OrcamentoController(IOrcamentoAppService orcamentoAppService, IMapper mapper)
        {
            _orcamentoAppService = orcamentoAppService;
            _mapper              = mapper;
        }

        /// <summary>Obt�m um or�amento pelo seu Id.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var response = await _orcamentoAppService.ObterPorIdAsync(id.ToString());
            return response.hasErrors ? BadRequest(response) : Ok(response);
        }

        /// <summary>Obt�m o or�amento vinculado a uma Ordem de Servi�o.</summary>
        [HttpGet("ordem-servico/{ordemServicoId:int}")]
        public async Task<IActionResult> ObterPorOrdemServico(int ordemServicoId)
        {
            var response = await _orcamentoAppService.ObterPorOrdemServicoIdAsync(ordemServicoId);
            return response.hasErrors ? NotFound(response) : Ok(response);
        }

        /// <summary>Realiza ajuste manual de valores no or�amento.</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> AtualizarManual(int id, [FromBody] OrcamentoUpdateViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var dto      = _mapper.Map<OrcamentoUpdateDto>(viewModel);
            var response = await _orcamentoAppService.AtualizarManualAsync(id.ToString(), dto);
            return response.hasErrors ? BadRequest(response) : Ok(response);
        }

        /// <summary>Remove o or�amento (geralmente ao cancelar a OS).</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Remover(int id)
        {
            var response = await _orcamentoAppService.RemoverAsync(id.ToString());
            return response.hasErrors ? BadRequest(response) : Ok(response);
        }

        /// <summary>Exporta o or�amento como relat�rio em PDF.</summary>
        [HttpGet("{id:int}/pdf")]
        public async Task<IActionResult> ExportarPdf(int id)
        {
            var response = await _orcamentoAppService.ExportarPdfAsync(id);

            if (response.hasErrors)
                return BadRequest(response);

            return File(
                response.getResponse,
                "application/pdf",
                $"orcamento-{id:D6}.pdf");
        }
    }
}