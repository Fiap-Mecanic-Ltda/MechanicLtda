using MechanicLtda.API.Controllers.Base;
using MechanicLtda.API.ViewModels;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicLtda.API.Controllers
{
    [ApiVersion("1.0")]
    public class AuthController : BaseController
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IAuthAppService _authAppService;

        public AuthController(INotificadorService notificadorService,
                              ILogger<AuthController> logger,
                              IAuthAppService authAppService) : base(notificadorService)
        {
            _logger = logger;
            _authAppService = authAppService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return CustomResponse(ModelState);

                return CustomResponse(await _authAppService.LoginAsync(model.Email, model.Senha));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao realizar login", _logger);
                return CustomResponse();
            }
        }

        //[AllowAnonymous]
        //[HttpPost("registrar")]
        //public async Task<IActionResult> Registrar([FromBody] RegistrarViewModel model)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //            return CustomResponse(ModelState);

        //        return CustomResponse(await _authAppService.RegistrarAsync(
        //            model.UserName,
        //            model.Email,
        //            model.Senha,
        //            model.Tipo));
        //    }
        //    catch (Exception ex)
        //    {
        //        GravaException(ex, "Falha ao registrar usuário", _logger);
        //        return CustomResponse();
        //    }
        //}

        [HttpPut("alterar-senha")]
        public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return CustomResponse(ModelState);

                return CustomResponse(await _authAppService.AlterarSenhaAsync(
                    model.Email,
                    model.SenhaAtual,
                    model.NovaSenha));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao alterar senha", _logger);
                return CustomResponse();
            }
        }
    }
}
