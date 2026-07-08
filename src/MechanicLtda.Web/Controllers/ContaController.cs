using MechanicLtda.Web.ViewModels;
using MechanicLtda.Application.AppServices.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MechanicLtda.Web.Controllers
{
    public class ContaController : RazorControllerBase
    {
        private readonly IAuthAppService _authAppService;

        public ContaController(IAuthAppService authAppService)
        {
            _authAppService = authAppService;
        }

        [AllowAnonymous]
        [HttpGet("login")]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToLocal(returnUrl);

            return View(new LoginRazorViewModel { ReturnUrl = returnUrl });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRazorViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var response = await _authAppService.LoginAsync(model.Email, model.Senha);
            if (response.hasErrors)
            {
                AddErrors(response);
                return View(model);
            }

            var principal = CriarPrincipal(response.getResponse.Token);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = response.getResponse.Expiracao
                });

            return RedirectToLocal(model.ReturnUrl);
        }

        [HttpPost("logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        private static ClaimsPrincipal CriarPrincipal(string token)
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var claims = jwt.Claims.Select(NormalizarClaim).ToList();

            return new ClaimsPrincipal(new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme,
                ClaimTypes.Name,
                ClaimTypes.Role));
        }

        private static Claim NormalizarClaim(Claim claim)
        {
            return claim.Type switch
            {
                JwtRegisteredClaimNames.Email => new Claim(ClaimTypes.Email, claim.Value),
                "userName" => new Claim(ClaimTypes.Name, claim.Value),
                "role" => new Claim(ClaimTypes.Role, claim.Value),
                ClaimTypes.Role => new Claim(ClaimTypes.Role, claim.Value),
                _ => claim
            };
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
    }
}

