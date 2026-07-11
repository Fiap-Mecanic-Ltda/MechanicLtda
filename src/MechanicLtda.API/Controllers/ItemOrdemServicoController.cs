using AutoMapper;
using MechanicLtda.Bootstrap.Authorization;
using MechanicLtda.API.Controllers.Base;
using MechanicLtda.API.ViewModels;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicLtda.API.Controllers
{
    [Route("api/ordem-servico/{ordemServicoId}/itens")]
    [Authorize(Roles = Roles.Admin)]
    public class ItemOrdemServicoController : BaseController
    {
        private readonly ILogger<ItemOrdemServicoController> _logger;
        private readonly IItemOrdemServicoAppService _itemAppService;
        private readonly IMapper _mapper;

        public ItemOrdemServicoController(
            INotificadorService notificadorService,
            ILogger<ItemOrdemServicoController> logger,
            IItemOrdemServicoAppService itemAppService,
            IMapper mapper) : base(notificadorService)
        {
            _logger = logger;
            _itemAppService = itemAppService;
            _mapper = mapper;
        }

        /// <summary>Retorna todos os itens de uma Ordem de Serviço específica.</summary>
        [HttpGet]
        public async Task<IActionResult> ObterPorOrdemServico(int ordemServicoId)
        {
            try
            {
                return CustomResponse(await _itemAppService.ObterPorOrdemServicoIdAsync(ordemServicoId));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter itens da ordem de serviço", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Retorna um item pelo seu Id.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(int ordemServicoId, string id)
        {
            try
            {
                return CustomResponse(await _itemAppService.ObterPorIdAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter item", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Adiciona um novo item à Ordem de Serviço e recalcula o ValorTotalEstimado da OS.</summary>
        [HttpPost]
        public async Task<IActionResult> Criar(int ordemServicoId, [FromBody] ItemOrdemServicoCreateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return CustomResponse(ModelState);

                var dto = _mapper.Map<ItemOrdemServicoCreateDto>(model);
                return CustomResponse(await _itemAppService.AdicionarAsync(ordemServicoId, dto));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao adicionar item à ordem de serviço", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Atualiza um item existente e recalcula o ValorTotalEstimado da OS.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Atualizar(int ordemServicoId, string id, [FromBody] ItemOrdemServicoUpdateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return CustomResponse(ModelState);

                var dto = _mapper.Map<ItemOrdemServicoUpdateDto>(model);
                return CustomResponse(await _itemAppService.AtualizarAsync(id, dto));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao atualizar item da ordem de serviço", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Remove um item e recalcula o ValorTotalEstimado da OS.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Remover(int ordemServicoId, string id)
        {
            try
            {
                return CustomResponse(await _itemAppService.RemoverAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao remover item da ordem de serviço", _logger);
                return CustomResponse();
            }
        }
    }
}