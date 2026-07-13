namespace MechanicLtda.Domain.Interfaces.Services
{
    public interface IEmailService
    {
        Task EnviarEmailAsync(string destinatarioEmail, string destinatarioNome, string assunto, string corpoHtml);
    }
}
