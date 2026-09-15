using MechanicLtda.Domain.Interfaces.Services;
using MechanicLtda.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MechanicLtda.Bootstrap.Extensions
{
    public static class CpfCnpjHashBackfillExtension
    {
        /// <summary>
        /// Preenche o índice cego (CpfCnpjHash) dos clientes gravados antes da coluna existir.
        /// Idempotente: só toca em linhas com hash nulo, então pode rodar em todo start.
        /// </summary>
        public static async Task BackfillCpfCnpjHashAsync(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var sp          = scope.ServiceProvider;

            var context = sp.GetRequiredService<BancoAPIContext>();
            var hasher  = sp.GetRequiredService<IDocumentoHashService>();
            var logger  = sp.GetRequiredService<ILogger<BancoAPIContext>>();

            if (!hasher.EstaConfigurado)
            {
                logger.LogWarning(
                    "Backfill do CpfCnpjHash ignorado: chave do hash não configurada (CPF_HASH_KEY / Encryption:CpfCnpjHashKey).");
                return;
            }

            var pendentes = await context.Clientes
                .Where(c => c.CpfCnpjHash == null)
                .ToListAsync();

            var atualizados = 0;

            foreach (var cliente in pendentes)
            {
                if (string.IsNullOrWhiteSpace(cliente.CpfCnpj))
                    continue;

                cliente.CpfCnpjHash = hasher.GerarHash(cliente.CpfCnpj);
                atualizados++;
            }

            if (atualizados == 0)
                return;

            try
            {
                await context.SaveChangesAsync();

                logger.LogInformation("Backfill do CpfCnpjHash concluído para {Total} cliente(s).", atualizados);
            }
            catch (Exception ex)
            {
                // O índice do hash é único: se a base tiver dois clientes com o mesmo
                // CPF/CNPJ, a gravação falha. Isso é problema de qualidade de dado e precisa
                // de intervenção manual — não é motivo para deixar a API fora do ar.
                logger.LogError(ex,
                    "Backfill do CpfCnpjHash falhou. Verifique clientes com CPF/CNPJ duplicado; a autenticação por CPF não encontrará esses clientes até a correção.");
            }
        }
    }
}
