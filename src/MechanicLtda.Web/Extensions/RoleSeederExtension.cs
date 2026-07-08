using MechanicLtda.Web.Authorization;
using Microsoft.AspNetCore.Identity;

namespace MechanicLtda.Web.Extensions
{
    public static class RoleSeederExtension
    {
        public static async Task SeedRolesAsync(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = [Roles.Administrador, Roles.Funcionario, Roles.Cliente];

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}

