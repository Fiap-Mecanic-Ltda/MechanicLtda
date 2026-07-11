using Microsoft.AspNetCore.Authentication.Cookies;

namespace MechanicLtda.Web.Extensions
{
    public static class AuthenticationExtension
    {
        public static IServiceCollection AddCookieAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.LoginPath         = "/login";
                    options.LogoutPath        = "/logout";
                    options.AccessDeniedPath  = "/login";
                    options.Cookie.Name       = "MechanicLtda.Auth";
                    options.Cookie.HttpOnly   = true;
                    options.Cookie.SameSite   = SameSiteMode.Lax;
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan    = TimeSpan.FromHours(8);
                });

            return services;
        }
    }
}

