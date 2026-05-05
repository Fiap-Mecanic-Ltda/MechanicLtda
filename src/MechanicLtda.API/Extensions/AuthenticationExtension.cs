using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MechanicLtda.API.Extensions
{
    public static class AuthenticationExtension
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(); // Sem lambda: configuração aplicada via Configure<> abaixo

            // Configure<> registra um delegate que executa de forma LAZY,
            // somente quando JwtBearerOptions é resolvido pela primeira vez.
            // Nesse ponto, toda a configuração (incluindo a injetada pela
            // WebApplicationFactory nos testes) já foi aplicada ao IConfiguration.
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var jwtSettings = configuration.GetSection("JwtSettings");

                var secretKeyValue = jwtSettings["SecretKey"]
                    ?? throw new InvalidOperationException(
                        "JwtSettings:SecretKey não está configurada. " +
                        "Verifique o appsettings.json, User Secrets ou variáveis de ambiente.");

                var secretKey = Encoding.UTF8.GetBytes(secretKeyValue);

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey  = true,
                    ValidIssuer              = jwtSettings["Issuer"],
                    ValidAudience            = jwtSettings["Audience"],
                    IssuerSigningKey         = new SymmetricSecurityKey(secretKey),
                    ClockSkew                = TimeSpan.Zero
                };
            });

            return services;
        }
    }
}