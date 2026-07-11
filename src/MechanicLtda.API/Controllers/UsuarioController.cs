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
    [Authorize(Roles = Roles.Administrador)]
    public class UsuarioController : BaseController
    {
        private readonly ILogger<UsuarioController> _logger;
        private readonly IUsuarioAppService         _usuarioAppService;
        private readonly IMapper                    _mapper;

        public UsuarioController(INotificadorService        notificadorService,
                                 ILogger<UsuarioController> logger,
                                 IUsuarioAppService         usuarioAppService,
                                 IMapper                    mapper) : base(notificadorService)
        {
            _logger            = logger;
            _usuarioAppService = usuarioAppService;
            _mapper            = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> ObterTodos()
        {
            try
            {
                return CustomResponse(await _usuarioAppService.ObterTodosAsync());
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter usuários", _logger);
                return CustomResponse();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] UsuarioCreateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return CustomResponse(ModelState);

                var dto = _mapper.Map<UsuarioCreateDto>(model);
                return CustomResponse(await _usuarioAppService.AdicionarAsync(dto));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao criar usuário", _logger);
                return CustomResponse();
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Atualizar(string id, [FromBody] UsuarioUpdateViewModel model)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            try
            {
                var dto = _mapper.Map<UsuarioUpdateDto>(model);
                return CustomResponse(await _usuarioAppService.AtualizarAsync(id, dto));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao atualizar usuário", _logger);
                return CustomResponse();
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remover(string id)
        {
            try
            {
                return CustomResponse(await _usuarioAppService.RemoverAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao remover usuário", _logger);
                return CustomResponse();
            }
        }
    }
}
