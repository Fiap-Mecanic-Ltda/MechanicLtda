using MechanicLtda.Application.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
            .AddJwtBearer();

            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var jwtSettings = configuration.GetSection("JwtSettings");

                var secretKeyValue = JwtSecretProvider.GetSecretKey(configuration);

                var secretKey = Encoding.UTF8.GetBytes(secretKeyValue);

                // Dois emissores validos: a propria API (login e-mail/senha) e a Function
                // serverless de autenticacao por CPF, que assina com a mesma chave simetrica.
                var issuers = new[] { jwtSettings["Issuer"], jwtSettings["IssuerCpf"] }
                    .Where(issuer => !string.IsNullOrWhiteSpace(issuer))
                    .ToArray();

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey  = true,
                    ValidIssuers             = issuers,
                    ValidAudience            = jwtSettings["Audience"],
                    IssuerSigningKey         = new SymmetricSecurityKey(secretKey),
                    ClockSkew                = TimeSpan.Zero
                };
            });

            return services;
        }
    }
}

