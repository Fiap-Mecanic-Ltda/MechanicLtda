using MechanicLtda.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace MechanicLtda.Application.Security
{
    /// <summary>
    /// Índice cego do CPF/CNPJ: HMAC-SHA256 dos dígitos do documento.
    /// O CpfCnpj é persistido cifrado com IV aleatório (CpfCnpjEncryptionConverter), ou seja,
    /// o mesmo documento gera textos cifrados diferentes e não há como fazer
    /// "WHERE CpfCnpj = @cpf". O hash resolve exatamente esse caso: é estável para o mesmo
    /// documento, não revela o documento original e pode ser indexado.
    ///
    /// A chave é resolvida sob demanda (e não no construtor) para que a aplicação suba mesmo
    /// num ambiente em que o segredo ainda não foi publicado. Nesse caso, as operações que
    /// dependem do hash falham de forma explícita, em vez de gravar cliente sem índice.
    /// </summary>
    public class DocumentoHashService : IDocumentoHashService
    {
        private readonly IConfiguration _configuration;

        public DocumentoHashService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool EstaConfigurado => ObterChaveOuNulo(_configuration) is not null;

        public string Normalizar(string documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
                return string.Empty;

            var digitos = new StringBuilder(documento.Length);

            foreach (var caractere in documento)
            {
                if (char.IsDigit(caractere))
                    digitos.Append(caractere);
            }

            return digitos.ToString();
        }

        public string GerarHash(string documento)
        {
            var digitos = Normalizar(documento);

            if (digitos.Length == 0)
                throw new ArgumentException("Documento sem dígitos para gerar o hash.", nameof(documento));

            var chave = ObterChaveOuNulo(_configuration)
                ?? throw new InvalidOperationException(
                    "A chave do hash de CPF/CNPJ não está configurada. Defina 'CPF_HASH_KEY' como variável de ambiente ou 'Encryption:CpfCnpjHashKey' em configuração.");

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(chave));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(digitos));

            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static string? ObterChaveOuNulo(IConfiguration configuration)
        {
            var chave = Environment.GetEnvironmentVariable("CPF_HASH_KEY")
                        ?? configuration["Encryption:CpfCnpjHashKey"];

            return string.IsNullOrWhiteSpace(chave) ? null : chave;
        }
    }
}
