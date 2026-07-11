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
    [Route("api/servicos-oficina")]
    [Authorize(Roles = Roles.Admin)]
    public class ServicoOficinaController : BaseController
    {
        private readonly ILogger<ServicoOficinaController> _logger;
        private readonly IServicoOficinaAppService _servicoAppService;
        private readonly IMapper _mapper;

        public ServicoOficinaController(
            INotificadorService notificadorService,
            ILogger<ServicoOficinaController> logger,
            IServicoOficinaAppService servicoAppService,
            IMapper mapper) : base(notificadorService)
        {
            _logger = logger;
            _servicoAppService = servicoAppService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> ObterTodos()
        {
            try
            {
                return CustomResponse(await _servicoAppService.ObterTodosAsync());
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter serviços da oficina", _logger);
                return CustomResponse();
            }
        }

        [HttpGet("ativos")]
        public async Task<IActionResult> ObterAtivos()
        {
            try
            {
                return CustomResponse(await _servicoAppService.ObterAtivosAsync());
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter serviços ativos da oficina", _logger);
                return CustomResponse();
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(string id)
        {
            try
            {
                return CustomResponse(await _servicoAppService.ObterPorIdAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter serviço da oficina", _logger);
                return CustomResponse();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] ServicoOficinaCreateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return CustomResponse(ModelState);

                var dto = _mapper.Map<ServicoOficinaCreateDto>(model);
                return CustomResponse(await _servicoAppService.AdicionarAsync(dto));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao cadastrar serviço da oficina", _logger);
                return CustomResponse();
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Atualizar(string id, [FromBody] ServicoOficinaUpdateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return CustomResponse(ModelState);

                var dto = _mapper.Map<ServicoOficinaUpdateDto>(model);
                return CustomResponse(await _servicoAppService.AtualizarAsync(id, dto));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao atualizar serviço da oficina", _logger);
                return CustomResponse();
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remover(string id)
        {
            try
            {
                return CustomResponse(await _servicoAppService.RemoverAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao remover serviço da oficina", _logger);
                return CustomResponse();
            }
        }
    }
}
