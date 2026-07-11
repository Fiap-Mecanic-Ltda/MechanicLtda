using MechanicLtda.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Web.Extensions
{
    public static class MigrationExtension
    {
        public static async Task MigrateDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BancoAPIContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<BancoAPIContext>>();

            if (!context.Database.IsRelational())
            {
                await context.Database.EnsureCreatedAsync();
                logger.LogInformation("Banco InMemory inicializado via EnsureCreated.");
                return;
            }

            const int maxTentativas = 10;
            const int intervaloMs = 3000;

            for (int tentativa = 1; tentativa <= maxTentativas; tentativa++)
            {
                try
                {
                    logger.LogInformation("Aplicando migrations... (tentativa {Tentativa}/{Max})", tentativa, maxTentativas);
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Migrations aplicadas com sucesso.");
                    return;
                }
                catch (Exception ex) when (tentativa < maxTentativas)
                {
                    logger.LogWarning("Banco indisponível. Aguardando {Intervalo}ms. Erro: {Mensagem}", intervaloMs, ex.Message);
                    await Task.Delay(intervaloMs);
                }
            }

            await context.Database.MigrateAsync();
        }
    }
}

