namespace MechanicLtda.API.Extensions
{
    public static class EmailExtension
    {
        public static IServiceCollection AddEmailService(this IServiceCollection services, IConfiguration configuration)
        {
            var emailSettings = configuration.GetSection("EmailSettings");

            if (string.IsNullOrWhiteSpace(emailSettings["Host"]))
                throw new InvalidOperationException("EmailSettings:Host não está configurado.");
            if (string.IsNullOrWhiteSpace(emailSettings["RemetenteEmail"]))
                throw new InvalidOperationException("EmailSettings:RemetenteEmail não está configurado.");

            return services;
        }
    }
}
