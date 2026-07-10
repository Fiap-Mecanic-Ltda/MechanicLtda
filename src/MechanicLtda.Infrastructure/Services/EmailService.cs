using MailKit.Net.Smtp;
using MailKit.Security;
using MechanicLtda.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace MechanicLtda.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarEmailAsync(string destinatarioEmail, string destinatarioNome, string assunto, string corpoHtml)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            var host           = emailSettings["Host"];
            var port           = int.Parse(emailSettings["Port"] ?? "587");
            var user           = emailSettings["User"];
            var password       = emailSettings["Password"];
            var useSsl         = bool.Parse(emailSettings["UseSsl"] ?? "true");
            var remetenteNome  = emailSettings["RemetenteNome"] ?? "MechanicLtda";
            var remetenteEmail = emailSettings["RemetenteEmail"];

            var mensagem = new MimeMessage();
            mensagem.From.Add(new MailboxAddress(remetenteNome, remetenteEmail));
            mensagem.To.Add(new MailboxAddress(destinatarioNome, destinatarioEmail));
            mensagem.Subject = assunto;
            mensagem.Body = new TextPart("html") { Text = corpoHtml };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, useSsl ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.None);

            if (!string.IsNullOrEmpty(user))
                await client.AuthenticateAsync(user, password);

            await client.SendAsync(mensagem);
            await client.DisconnectAsync(true);
        }
    }
}
