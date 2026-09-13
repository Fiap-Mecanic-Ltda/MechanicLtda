using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace MechanicLtda.Bootstrap.Extensions
{
    public static class LoggingExtension
    {
        /// <summary>
        /// Fora do ambiente de desenvolvimento, os logs saem em JSON — um objeto por linha, com
        /// horário UTC, nível, categoria, mensagem, os valores estruturados e os escopos. Entre
        /// os escopos está o CorrelationId do middleware de correlação, que é o requestId do
        /// API Gateway: com ele, uma linha de log leva direto à requisição que a gerou.
        ///
        /// Em desenvolvimento continua o console legível, para quem acompanha no terminal.
        /// </summary>
        public static WebApplicationBuilder AddLogsEstruturados(this WebApplicationBuilder builder)
        {
            if (builder.Environment.IsDevelopment())
                return builder;

            builder.Logging.ClearProviders();
            builder.Logging.AddJsonConsole(options =>
            {
                options.IncludeScopes   = true;
                options.UseUtcTimestamp = true;
                options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

                options.JsonWriterOptions = new JsonWriterOptions
                {
                    Indented = false,

                    // Mantém acentos legíveis nos logs ("Diagnóstico", e não o
                    // escape Unicode que o encoder padrão gera).
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                };
            });

            return builder;
        }
    }
}
