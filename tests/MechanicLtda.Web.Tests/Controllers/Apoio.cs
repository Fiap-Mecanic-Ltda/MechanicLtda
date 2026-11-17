using MechanicLtda.Application.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace MechanicLtda.Web.Tests.Controllers
{
    /// <summary>Monta o contexto mínimo (HttpContext, TempData, autenticação) para testar controllers MVC.</summary>
    internal static class Apoio
    {
        public static T Preparar<T>(T controller, Mock<IAuthenticationService>? autenticacao = null) where T : Controller
        {
            var servicos = new ServiceCollection();
            servicos.AddSingleton((autenticacao ?? new Mock<IAuthenticationService>()).Object);
            var http = new DefaultHttpContext { RequestServices = servicos.BuildServiceProvider() };

            controller.ControllerContext = new ControllerContext { HttpContext = http };
            controller.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
            controller.Url = Mock.Of<IUrlHelper>();
            return controller;
        }

        public static ResponseDto<T> Ok<T>(T valor) => new ResponseDto<T>().setResponse(valor);

        public static ResponseDto<T> Erro<T>(string mensagem = "falhou") => new ResponseDto<T>().addError(mensagem);

        public static T InvalidarModelo<T>(this T controller) where T : Controller
        {
            controller.ModelState.AddModelError("id", "O valor 'x' não é válido.");
            return controller;
        }

        public static RedirectToActionResult Redireciona(IActionResult resultado, string acao, string? controller = null)
        {
            var redirect = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal(acao, redirect.ActionName);
            Assert.Equal(controller, redirect.ControllerName);
            return redirect;
        }

        public static TModel Modelo<TModel>(IActionResult resultado)
        {
            var view = Assert.IsType<ViewResult>(resultado);
            return Assert.IsAssignableFrom<TModel>(view.Model);
        }
    }
}
