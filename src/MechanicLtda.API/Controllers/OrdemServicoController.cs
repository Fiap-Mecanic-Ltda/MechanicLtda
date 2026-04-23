using AutoMapper;
using MechanicLtda.API.Controllers.Base;
using MechanicLtda.API.ViewModels;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace MechanicLtda.API.Controllers
{
    public class OrdemServicoController : BaseController
    {
        private readonly ILogger<OrdemServicoController> _logger;
        private readonly IOrdemServicoAppService _ordemServicoAppService;
        private readonly IMapper _mapper;

        public OrdemServicoController(INotificadorService notificadorService,
                                      ILogger<OrdemServicoController> logger,
                                      IOrdemServicoAppService ordemServicoAppService,
                                      IMapper mapper) : base(notificadorService)
        {
            _logger = logger;
            _ordemServicoAppService = ordemServicoAppService;
            _mapper = mapper;
        }

        /// <summary>Lista todas as Ordens de Serviço.</summary>
        [HttpGet]
        public async Task<IActionResult> ObterTodos()
        {
            try
            {
                return CustomResponse(await _ordemServicoAppService.ObterTodosAsync());
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter ordens de serviço", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Retorna uma Ordem de Serviço pelo Id.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(string id)
        {
            try
            {
                return CustomResponse(await _ordemServicoAppService.ObterPorIdAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter ordem de serviço", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Lista as Ordens de Serviço filtradas pelo ClienteId.</summary>
        [HttpGet("cliente/{clienteId}")]
        public async Task<IActionResult> ObterPorCliente(string clienteId)
        {
            try
            {
                return CustomResponse(await _ordemServicoAppService.ObterPorClienteIdAsync(clienteId));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter ordens de serviço do cliente", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Cria uma nova Ordem de Serviço (status inicial: Em Aberto).</summary>
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] OrdemServicoCreateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return CustomResponse(ModelState);

                var dto = _mapper.Map<OrdemServicoCreateDto>(model);
                return CustomResponse(await _ordemServicoAppService.AdicionarAsync(dto));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao criar ordem de serviço", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Atualiza uma Ordem de Serviço. Avança automaticamente para 'Em Validação' quando todos os campos estiverem preenchidos.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Atualizar(string id, [FromBody] OrdemServicoUpdateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return CustomResponse(ModelState);

                var dto = _mapper.Map<OrdemServicoUpdateDto>(model);
                return CustomResponse(await _ordemServicoAppService.AtualizarAsync(id, dto));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao atualizar ordem de serviço", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Move manualmente a OS para o status 'Em Validação'. Prepara o gatilho para geração de Orçamento.</summary>
        [HttpPatch("{id}/em-validacao")]
        public async Task<IActionResult> MoverParaEmValidacao(string id)
        {
            try
            {
                return CustomResponse(await _ordemServicoAppService.MoverParaEmValidacaoAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao mover ordem de serviço para Em Validação", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Remove uma Ordem de Serviço.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Remover(string id)
        {
            try
            {
                return CustomResponse(await _ordemServicoAppService.RemoverAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao remover ordem de serviço", _logger);
                return CustomResponse();
            }
        }
    }
}