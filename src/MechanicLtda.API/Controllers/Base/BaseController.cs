using MechanicLtda.Application.DTOs;
using MechanicLtda.Bootstrap.Authorization;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MechanicLtda.API.Controllers.Base
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BaseController : ControllerBase
    {
        private readonly INotificadorService _notificadorService;

        protected BaseController(INotificadorService notificadorService)
        {
            _notificadorService = notificadorService;
        }

        protected ActionResult CustomResponse<T>(ResponseDto<T> result)
        {
            if (result.hasErrors)
            {
                foreach (var errorMsg in result.getErrors)
                    NotificarErro(errorMsg);

                return BadRequest(new
                {
                    success = false,
                    errors = _notificadorService.ObterNotificacoes().Select(n => n.Mensagem)
                });
            }

            return Ok(result.getResponse);
        }

        protected ActionResult CustomResponse(object result = null)
        {
            if (OperacaoValida())
            {
                return Ok(result);
            }

            return BadRequest(new
            {
                success = false,
                errors = _notificadorService.ObterNotificacoes().Select(n => n.Mensagem)
            });
        }

        protected ActionResult CustomResponse(ModelStateDictionary modelState)
        {
            if (!modelState.IsValid) NotificarErroModelInvalida(modelState);
            return CustomResponse();
        }

        protected void NotificarErroModelInvalida(ModelStateDictionary modelState)
        {
            var erros = modelState.Values.SelectMany(e => e.Errors);
            foreach (var erro in erros)
            {
                var errorMsg = erro.Exception == null ? erro.ErrorMessage : erro.Exception.Message;
                NotificarErro(errorMsg);
            }
        }

        protected bool OperacaoValida()
        {
            return !_notificadorService.TemNotificacao();
        }

        protected void NotificarErro(string mensagem)
        {
            _notificadorService.Handle(new Notificacao(mensagem));
        }

        /// <summary>
        /// Regra de posse do recurso: Administrador e Funcionário acessam qualquer cliente;
        /// o token emitido por CPF (role Cliente) só acessa o próprio clienteId, que vem no
        /// claim do token. Sem isso, um cliente autenticado leria os dados de outro.
        /// </summary>
        protected bool PodeAcessarCliente(string clienteId)
        {
            if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.Funcionario))
                return true;

            var clienteIdDoToken = User.FindFirst("clienteId")?.Value;

            return !string.IsNullOrWhiteSpace(clienteIdDoToken)
                   && string.Equals(clienteIdDoToken, clienteId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>403 no mesmo envelope de erro usado pelo restante da API.</summary>
        protected ActionResult ClienteSemPermissao()
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                success = false,
                errors = new[] { "O cliente autenticado só pode consultar os próprios dados." }
            });
        }

        protected void GravaException(Exception ex, string mensagem, ILogger _logger)
        {
            _logger.LogError(ex, mensagem);
            NotificarErro(mensagem);
        }
    }
}