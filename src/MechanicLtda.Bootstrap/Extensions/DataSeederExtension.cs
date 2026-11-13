using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;
using MechanicLtda.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MechanicLtda.Bootstrap.Extensions
{
    public static class DataSeederExtension
    {
        /// <summary>
        /// Aplica migrations pendentes e insere dados mocados caso o banco esteja vazio.
        /// </summary>
        public static async Task SeedDataAsync(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var sp          = scope.ServiceProvider;

            var context     = sp.GetRequiredService<BancoAPIContext>();
            var userManager = sp.GetRequiredService<UserManager<Usuario>>();

            // Aplica migrations apenas em provedores relacionais (SQL Server, etc.)
            // Em testes com InMemory, usa EnsureCreated para criar o schema
            if (context.Database.IsRelational())
                await context.Database.MigrateAsync();
            else
                await context.Database.EnsureCreatedAsync();

            // Evita re-seed caso já existam dados
            if (await context.Clientes.AnyAsync())
                return;

            // ── Usuário admin ────────────────────────────────────────────────
            if (await userManager.FindByEmailAsync("admin@mechanic.com") is null)
            {
                var admin = new Usuario
                {
                    UserName    = "admin",
                    Email       = "admin@mechanic.com",
                    Ativo       = true,
                    Tipo        = TipoUsuario.Administrador,
                    DataCriacao = DateTime.UtcNow
                };
                await userManager.CreateAsync(admin, "Admin@123");
                await userManager.AddToRoleAsync(admin, "Administrador");
            }

            // ── Clientes ─────────────────────────────────────────────────────
            // Os CPFs precisam passar no cálculo dos dígitos verificadores: a autenticação
            // por CPF (Function serverless) valida o documento antes de consultar a base, e
            // CPF inválido nem chega ao banco. O último cliente entra inativo de propósito,
            // para demonstrar a recusa por status.
            var clientes = new List<Cliente>
            {
                new() { Nome = "Carlos Oliveira",  Email = "carlos@email.com",  Telefone = "11999990001", CpfCnpj = "123.456.789-09", Ativo = true,  DataCriacao = DateTime.UtcNow },
                new() { Nome = "Fernanda Lima",    Email = "fernanda@email.com", Telefone = "11999990002", CpfCnpj = "529.982.247-25", Ativo = true,  DataCriacao = DateTime.UtcNow },
                new() { Nome = "Ricardo Souza",    Email = "ricardo@email.com",  Telefone = "11999990003", CpfCnpj = "111.444.777-35", Ativo = true,  DataCriacao = DateTime.UtcNow },
                new() { Nome = "Marina Duarte",    Email = "marina@email.com",   Telefone = "11999990004", CpfCnpj = "987.654.321-00", Ativo = false, DataCriacao = DateTime.UtcNow }
            };

            context.Clientes.AddRange(clientes);
            await context.SaveChangesAsync();

            // ── Veículos ─────────────────────────────────────────────────────
            var veiculos = new List<Veiculo>
            {
                new() { Placa = "ABC1D23", Marca = "Toyota",     Modelo = "Corolla",  Ano = 2021, Ativo = true, ClienteId = clientes[0].Id, DataCriacao = DateTime.UtcNow },
                new() { Placa = "DEF4E56", Marca = "Honda",      Modelo = "Civic",    Ano = 2019, Ativo = true, ClienteId = clientes[1].Id, DataCriacao = DateTime.UtcNow },
                new() { Placa = "GHI7F89", Marca = "Volkswagen", Modelo = "Polo",     Ano = 2022, Ativo = true, ClienteId = clientes[2].Id, DataCriacao = DateTime.UtcNow },
                new() { Placa = "JKL0G12", Marca = "Chevrolet",  Modelo = "Onix",     Ano = 2020, Ativo = true, ClienteId = clientes[0].Id, DataCriacao = DateTime.UtcNow }
            };

            context.Veiculos.AddRange(veiculos);
            await context.SaveChangesAsync();
        }
    }
}
