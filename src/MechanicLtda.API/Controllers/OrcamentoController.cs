using AutoMapper;
using MechanicLtda.API.ViewModels;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicLtda.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrcamentoController : ControllerBase
    {
        private readonly IOrcamentoAppService _orcamentoAppService;
        private readonly IMapper              _mapper;

        public OrcamentoController(IOrcamentoAppService orcamentoAppService, IMapper mapper)
        {
            _orcamentoAppService = orcamentoAppService;
            _mapper              = mapper;
        }

        /// <summary>Obtém um orçamento pelo seu Id.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var response = await _orcamentoAppService.ObterPorIdAsync(id.ToString());
            return response.hasErrors ? BadRequest(response) : Ok(response);
        }

        /// <summary>Obtém o orçamento vinculado a uma Ordem de Serviço.</summary>
        [HttpGet("ordem-servico/{ordemServicoId:int}")]
        public async Task<IActionResult> ObterPorOrdemServico(int ordemServicoId)
        {
            var response = await _orcamentoAppService.ObterPorOrdemServicoIdAsync(ordemServicoId);
            return response.hasErrors ? NotFound(response) : Ok(response);
        }

        /// <summary>Realiza ajuste manual de valores no orçamento.</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> AtualizarManual(int id, [FromBody] OrcamentoUpdateViewModel viewModel)
        {
            var dto      = _mapper.Map<OrcamentoUpdateDto>(viewModel);
            var response = await _orcamentoAppService.AtualizarManualAsync(id.ToString(), dto);
            return response.hasErrors ? BadRequest(response) : Ok(response);
        }

        /// <summary>Remove o orçamento (geralmente ao cancelar a OS).</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Remover(int id)
        {
            var response = await _orcamentoAppService.RemoverAsync(id.ToString());
            return response.hasErrors ? BadRequest(response) : Ok(response);
        }
    }
}