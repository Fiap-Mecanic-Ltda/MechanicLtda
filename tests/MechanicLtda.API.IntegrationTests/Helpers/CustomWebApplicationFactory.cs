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

    public const string ClienteEmail = "cliente@mechanic.com";
    public const string ClienteSenha = "Cliente@123";

    public const string FuncionarioEmail = "funcionario@mechanic.com";
    public const string FuncionarioSenha = "Funcionario@123";

    private readonly string                _dbName  = $"TestDb_{Guid.NewGuid()}";
    private readonly InMemoryDatabaseRoot  _dbRoot  = new();

    public CustomWebApplicationFactory()
    {
        // Garante que a variável de ambiente esteja disponível para GerarTokenAsync
        Environment.SetEnvironmentVariable("JWT_SECRET_KEY", "ChaveSecretaDeTeste_MechanicLtda_2024!");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
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

        var userManager = sp.GetRequiredService<UserManager<Usuario>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();

        SeedUsuarioAsync(userManager, roleManager, AdminEmail, AdminSenha, "admin", TipoUsuario.Administrador, "Administrador")
            .GetAwaiter().GetResult();
        SeedUsuarioAsync(userManager, roleManager, FuncionarioEmail, FuncionarioSenha, "funcionario", TipoUsuario.Funcionario, "Funcionario")
            .GetAwaiter().GetResult();
        SeedUsuarioAsync(userManager, roleManager, ClienteEmail, ClienteSenha, "cliente", TipoUsuario.Cliente, "Cliente")
            .GetAwaiter().GetResult();

        return host;
    }

    private static async Task SeedUsuarioAsync(
        UserManager<Usuario>      userManager,
        RoleManager<IdentityRole> roleManager,
        string                    email,
        string                    senha,
        string                    userName,
        TipoUsuario               tipo,
        string                    role)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var usuario = new Usuario
        {
            UserName    = userName,
            Email       = email,
            Ativo       = true,
            Tipo        = tipo,
            DataCriacao = DateTime.UtcNow   // campo [Required] — obrigatório para CreateAsync ter sucesso
        };

        var result = await userManager.CreateAsync(usuario, senha);

        // Lança exceção explícita para não mascarar falha no seed
        if (!result.Succeeded)
        {
            var erros = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Falha ao criar usuário '{email}' no seed de testes: {erros}");
        }

        await userManager.AddToRoleAsync(usuario, role);
    }
}