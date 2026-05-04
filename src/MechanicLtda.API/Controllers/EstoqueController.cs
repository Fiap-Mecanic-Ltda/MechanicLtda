using AutoMapper;
using MechanicLtda.API.Authorization;
using MechanicLtda.API.Controllers.Base;
using MechanicLtda.API.ViewModels;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Enums;
using MechanicLtda.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicLtda.API.Controllers
{
    [Route("api/estoque")]
    [Authorize(Roles = Roles.Admin)]
    public class EstoqueController : BaseController
    {
        private readonly ILogger<EstoqueController> _logger;
        private readonly IEstoqueAppService _estoqueAppService;
        private readonly IMapper _mapper;

        public EstoqueController(
            INotificadorService notificadorService,
            ILogger<EstoqueController> logger,
            IEstoqueAppService estoqueAppService,
            IMapper mapper) : base(notificadorService)
        {
            _logger            = logger;
            _estoqueAppService = estoqueAppService;
            _mapper            = mapper;
        }

        /// <summary>Retorna todos os itens de estoque.</summary>
        [HttpGet]
        public async Task<IActionResult> ObterTodos()
        {
            try
            {
                return CustomResponse(await _estoqueAppService.ObterTodosAsync());
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter itens de estoque", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Retorna os itens de estoque filtrados por tipo (1 = Insumo, 2 = Peça).</summary>
        [HttpGet("tipo/{tipo}")]
        public async Task<IActionResult> ObterPorTipo(TipoEstoque tipo)
        {
            try
            {
                return CustomResponse(await _estoqueAppService.ObterPorTipoAsync(tipo));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao filtrar itens de estoque por tipo", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Retorna um item de estoque pelo seu Id.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(string id)
        {
            try
            {
                return CustomResponse(await _estoqueAppService.ObterPorIdAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter item de estoque", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Cadastra um novo item no estoque.</summary>
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] EstoqueCreateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return CustomResponse(ModelState);

                var dto = _mapper.Map<EstoqueCreateDto>(model);
                return CustomResponse(await _estoqueAppService.AdicionarAsync(dto));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao cadastrar item de estoque", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Atualiza dados cadastrais de um item de estoque (nome, tipo e quantidade mínima).</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Atualizar(string id, [FromBody] EstoqueUpdateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return CustomResponse(ModelState);

                var dto = _mapper.Map<EstoqueUpdateDto>(model);
                return CustomResponse(await _estoqueAppService.AtualizarAsync(id, dto));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao atualizar item de estoque", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Remove um item de estoque.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Remover(string id)
        {
            try
            {
                return CustomResponse(await _estoqueAppService.RemoverAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao remover item de estoque", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Repõe estoque de um item (entrada de nota/compra).</summary>
        [HttpPatch("{id}/reposicao")]
        public async Task<IActionResult> ReporEstoque(string id, [FromBody] EstoqueReposicaoViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return CustomResponse(ModelState);

                var dto = _mapper.Map<EstoqueReposicaoDto>(model);
                return CustomResponse(await _estoqueAppService.ReporQuantidadeAsync(id, dto));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao repor estoque", _logger);
                return CustomResponse();
            }
        }
    }
}