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
            _logger         = logger;
            _authAppService = authAppService;
        }

        /// <summary>Realiza login e retorna o token JWT.</summary>
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

        /// <summary>
        /// Registra um novo usuário. Tipos disponíveis: 1 = Administrador, 2 = Funcionario, 3 = Cliente.
        /// O role JWT é atribuído automaticamente conforme o tipo informado.
        /// </summary>
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

        /// <summary>Altera a senha do usuário autenticado.</summary>
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
