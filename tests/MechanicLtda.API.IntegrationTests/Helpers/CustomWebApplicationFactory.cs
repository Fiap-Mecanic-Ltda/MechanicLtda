using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;
using MechanicLtda.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MechanicLtda.API.IntegrationTests.Helpers;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@mechanic.com";
    public const string AdminSenha = "Admin@123";

    // Root compartilhado: garante que TODOS os DbContext — independente do
    // service provider interno que cada um cria — acessem os mesmos dados.
    private readonly string                _dbName  = $"TestDb_{Guid.NewGuid()}";
    private readonly InMemoryDatabaseRoot  _dbRoot  = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["JwtSettings:SecretKey"]        = "ChaveSecretaDeTeste_MechanicLtda_2024!",
                ["JwtSettings:Issuer"]           = "MechanicLtda",
                ["JwtSettings:Audience"]         = "MechanicLtdaUsers",
                ["JwtSettings:ExpiracaoMinutos"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<BancoAPIContext>)
                         || d.ServiceType == typeof(BancoAPIContext)
                         || d.ServiceType == typeof(IDbContextOptionsConfiguration<BancoAPIContext>))
                .ToList();

            descriptors.ForEach(d => services.Remove(d));

            // _dbRoot é a mesma instância para todos os DbContext da factory.
            // EnableServiceProviderCaching(false) evita conflito com o provider
            // interno do SQL Server; _dbRoot garante que o banco InMemory seja
            // compartilhado mesmo com providers internos distintos.
            services.AddDbContext<BancoAPIContext>(options =>
                options
                    .UseInMemoryDatabase(_dbName, _dbRoot)
                    .EnableServiceProviderCaching(false));
        });

        builder.UseEnvironment("Development");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var sp          = scope.ServiceProvider;

        SeedAdminAsync(
            sp.GetRequiredService<UserManager<Usuario>>(),
            sp.GetRequiredService<RoleManager<IdentityRole>>())
            .GetAwaiter().GetResult();

        return host;
    }

    private static async Task SeedAdminAsync(
        UserManager<Usuario>      userManager,
        RoleManager<IdentityRole> roleManager)
    {
        const string role = "Administrador";

        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

        if (await userManager.FindByEmailAsync(AdminEmail) is not null)
            return;

        var admin = new Usuario
        {
            UserName    = "admin",
            Email       = AdminEmail,
            Ativo       = true,
            Tipo        = TipoUsuario.Administrador,
            DataCriacao = DateTime.UtcNow   // campo [Required] — obrigatório para CreateAsync ter sucesso
        };

        var result = await userManager.CreateAsync(admin, AdminSenha);

        // Lança exceção explícita para não mascarar falha no seed
        if (!result.Succeeded)
        {
            var erros = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Falha ao criar usuário admin no seed de testes: {erros}");
        }

        await userManager.AddToRoleAsync(admin, role);
    }
}