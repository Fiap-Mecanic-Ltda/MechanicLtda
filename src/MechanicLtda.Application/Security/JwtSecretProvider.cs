using Microsoft.Extensions.Configuration;

namespace MechanicLtda.Application.Security
{
    public static class JwtSecretProvider
    {
        public static string GetSecretKey(IConfiguration configuration)
        {
            return Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                   ?? configuration["JWT_SECRET_KEY"]
                   ?? configuration["JwtSettings:SecretKey"]
                   ?? throw new InvalidOperationException(
                       "A chave JWT não está configurada. Defina 'JWT_SECRET_KEY' como variável de ambiente ou em configuração.");
        }
    }
}
