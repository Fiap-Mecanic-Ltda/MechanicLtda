using MechanicLtda.Bootstrap.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace MechanicLtda.Bootstrap.Extensions
{
    public static class RoleSeederExtension
    {
        /// <summary>
        /// Garante que os roles padrão existem no banco antes de a aplicação
        /// começar a receber requisições.
        /// </summary>
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
