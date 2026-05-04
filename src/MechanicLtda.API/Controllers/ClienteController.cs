using AutoMapper;
using MechanicLtda.API.Authorization;
using MechanicLtda.API.Controllers.Base;
using MechanicLtda.API.ViewModels;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicLtda.API.Controllers
{
    [ApiVersion("1.0")]
    [Authorize(Roles = Roles.Admin)]
    public class ClienteController : BaseController
    {
        private readonly ILogger<ClienteController> _logger;
        private readonly IClienteAppService         _clienteAppService;
        private readonly IMapper                    _mapper;

        public ClienteController(INotificadorService        notificadorService,
                                 ILogger<ClienteController> logger,
                                 IClienteAppService         clienteAppService,
                                 IMapper                    mapper) : base(notificadorService)
        {
            _logger            = logger;
            _clienteAppService = clienteAppService;
            _mapper            = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> ObterTodos()
        {
            try
            {
                return CustomResponse(await _clienteAppService.ObterTodosAsync());
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter clientes", _logger);
                return CustomResponse();
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(string id)
        {
            try
            {
                return CustomResponse(await _clienteAppService.ObterPorIdAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter cliente", _logger);
                return CustomResponse();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] ClienteCreateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return CustomResponse(ModelState);

                var dto = _mapper.Map<ClienteCreateDto>(model);
                return CustomResponse(await _clienteAppService.AdicionarAsync(dto));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao criar cliente", _logger);
                return CustomResponse();
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Atualizar(string id, [FromBody] ClienteUpdateViewModel model)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            try
            {
                var dto = _mapper.Map<ClienteUpdateDto>(model);
                return CustomResponse(await _clienteAppService.AtualizarAsync(id, dto));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao atualizar cliente", _logger);
                return CustomResponse();
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remover(string id)
        {
            try
            {
                return CustomResponse(await _clienteAppService.RemoverAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao remover cliente", _logger);
                return CustomResponse();
            }
        }
    }
}