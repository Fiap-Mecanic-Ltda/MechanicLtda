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
    }
}

