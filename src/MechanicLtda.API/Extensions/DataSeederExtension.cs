using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;
using MechanicLtda.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.API.Extensions
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

            // Aplica migrations automaticamente (garante que o schema exista no container)
            await context.Database.MigrateAsync();

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
            var clientes = new List<Cliente>
            {
                new() { Nome = "Carlos Oliveira",  Email = "carlos@email.com",  Telefone = "11999990001", CpfCnpj = "123.456.789-01", Ativo = true, DataCriacao = DateTime.UtcNow },
                new() { Nome = "Fernanda Lima",    Email = "fernanda@email.com", Telefone = "11999990002", CpfCnpj = "234.567.890-12", Ativo = true, DataCriacao = DateTime.UtcNow },
                new() { Nome = "Ricardo Souza",    Email = "ricardo@email.com",  Telefone = "11999990003", CpfCnpj = "345.678.901-23", Ativo = true, DataCriacao = DateTime.UtcNow }
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

            // ── Estoque ──────────────────────────────────────────────────────
            var estoques = new List<Estoque>
            {
                new() { Nome = "Óleo Motor 5W30",      Tipo = TipoEstoque.Insumo, QuantidadeAtual = 50, QuantidadeMinima = 10, DataUltimaAtualizacao = DateTime.UtcNow },
                new() { Nome = "Filtro de Ar",          Tipo = TipoEstoque.Peca,   QuantidadeAtual = 30, QuantidadeMinima = 5,  DataUltimaAtualizacao = DateTime.UtcNow },
                new() { Nome = "Pastilha de Freio",     Tipo = TipoEstoque.Peca,   QuantidadeAtual = 20, QuantidadeMinima = 4,  DataUltimaAtualizacao = DateTime.UtcNow },
                new() { Nome = "Fluido de Freio DOT 4", Tipo = TipoEstoque.Insumo, QuantidadeAtual = 15, QuantidadeMinima = 3,  DataUltimaAtualizacao = DateTime.UtcNow },
                new() { Nome = "Correia Dentada",       Tipo = TipoEstoque.Peca,   QuantidadeAtual = 8,  QuantidadeMinima = 2,  DataUltimaAtualizacao = DateTime.UtcNow }
            };

            context.Estoques.AddRange(estoques);
            await context.SaveChangesAsync();

            // ── Ordens de Serviço ────────────────────────────────────────────
            var ordens = new List<OrdemServico>
            {
                new()
                {
                    ClienteId         = clientes[0].Id,
                    VeiculoId         = veiculos[0].Id,
                    DescricaoProblema = "Troca de óleo e filtro de ar.",
                    Status            = StatusOrdemServico.EmExecucao,
                    ValorTotalEstimado = 250.00m,
                    DataCriacao       = DateTime.UtcNow
                },
                new()
                {
                    ClienteId         = clientes[1].Id,
                    VeiculoId         = veiculos[1].Id,
                    DescricaoProblema = "Revisão de freios dianteiros e traseiros.",
                    Status            = StatusOrdemServico.Recebida,
                    ValorTotalEstimado = 420.00m,
                    DataCriacao       = DateTime.UtcNow
                },
                new()
                {
                    ClienteId         = clientes[2].Id,
                    VeiculoId         = veiculos[2].Id,
                    DescricaoProblema = "Substituição de correia dentada.",
                    Status            = StatusOrdemServico.AguardandoAprovacao,
                    ValorTotalEstimado = 680.00m,
                    DataCriacao       = DateTime.UtcNow
                }
            };

            context.OrdensServico.AddRange(ordens);
            await context.SaveChangesAsync();

            // ── Itens de Ordem de Serviço ────────────────────────────────────
            var itens = new List<ItemOrdemServico>
            {
                new() { OrdemServicoId = ordens[0].Id, EstoqueId = estoques[0].Id, Quantidade = 1, ValorUnitario = 45.00m,  ValorTotal = 45.00m  },
                new() { OrdemServicoId = ordens[0].Id, EstoqueId = estoques[1].Id, Quantidade = 1, ValorUnitario = 35.00m,  ValorTotal = 35.00m  },
                new() { OrdemServicoId = ordens[1].Id, EstoqueId = estoques[2].Id, Quantidade = 2, ValorUnitario = 120.00m, ValorTotal = 240.00m },
                new() { OrdemServicoId = ordens[1].Id, EstoqueId = estoques[3].Id, Quantidade = 1, ValorUnitario = 30.00m,  ValorTotal = 30.00m  },
                new() { OrdemServicoId = ordens[2].Id, EstoqueId = estoques[4].Id, Quantidade = 1, ValorUnitario = 280.00m, ValorTotal = 280.00m }
            };

            context.ItensOrdemServico.AddRange(itens);
            await context.SaveChangesAsync();

            // ── Orçamentos ───────────────────────────────────────────────────
            var orcamentos = new List<Orcamento>
            {
                new()
                {
                    OrdemServicoId    = ordens[0].Id,
                    ValorTotalInsumos = 45.00m,
                    ValorTotalPecas   = 35.00m,
                    ValorTotalGeral   = 80.00m,
                    DataGeracao       = DateTime.UtcNow,
                    Validade          = DateTime.UtcNow.AddDays(30)
                },
                new()
                {
                    OrdemServicoId    = ordens[1].Id,
                    ValorTotalInsumos = 30.00m,
                    ValorTotalPecas   = 240.00m,
                    ValorTotalGeral   = 270.00m,
                    DataGeracao       = DateTime.UtcNow,
                    Validade          = DateTime.UtcNow.AddDays(30)
                },
                new()
                {
                    OrdemServicoId    = ordens[2].Id,
                    ValorTotalInsumos = 0m,
                    ValorTotalPecas   = 280.00m,
                    ValorTotalGeral   = 280.00m,
                    DataGeracao       = DateTime.UtcNow,
                    Validade          = DateTime.UtcNow.AddDays(30)
                }
            };

            context.Orcamentos.AddRange(orcamentos);
            await context.SaveChangesAsync();
        }
    }
}