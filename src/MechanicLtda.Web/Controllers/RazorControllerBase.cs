using MechanicLtda.Application.DTOs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicLtda.Web.Controllers
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public abstract class RazorControllerBase : Controller
    {
        protected void AddErrors<T>(ResponseDto<T> response)
        {
            foreach (var error in response.getErrors)
                ModelState.AddModelError(string.Empty, error);
        }

        protected void FlashErrors<T>(ResponseDto<T> response)
        {
            if (response.hasErrors)
                TempData["Error"] = string.Join(" ", response.getErrors);
        }

        protected void FlashSuccess(string message)
        {
            TempData["Success"] = message;
        }

        /// <summary>
        /// Controller para onde volta uma requisição com parâmetros inválidos (ex.: id que não
        /// é número). Nulo = o próprio controller; quem não tem Index sobrescreve.
        /// </summary>
        protected virtual string? ControllerDeRetorno => null;

        /// <summary>Resposta padrão quando o ModelState da action não é válido.</summary>
        protected IActionResult RequisicaoInvalida()
        {
            var erros = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct();

            var mensagem = string.Join(" ", erros);
            TempData["Error"] = string.IsNullOrWhiteSpace(mensagem) ? "Requisição inválida." : mensagem;
            return RedirectToAction("Index", ControllerDeRetorno);
        }
    }
}

