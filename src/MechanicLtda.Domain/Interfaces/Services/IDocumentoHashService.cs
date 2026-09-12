namespace MechanicLtda.Domain.Interfaces.Services
{
    /// <summary>
    /// Gera o índice cego (hash determinístico) de um CPF/CNPJ.
    /// A coluna CpfCnpj é cifrada com IV aleatório, o que impede busca por igualdade no
    /// banco; o hash é determinístico e, por isso, pesquisável. É o que permite à Function
    /// serverless de autenticação localizar o cliente a partir do CPF informado.
    /// </summary>
    public interface IDocumentoHashService
    {
        /// <summary>Indica se a chave do hash está configurada no ambiente.</summary>
        bool EstaConfigurado { get; }

        /// <summary>Remove a máscara e devolve apenas os dígitos do documento.</summary>
        string Normalizar(string documento);

        /// <summary>HMAC-SHA256 (hexadecimal minúsculo) dos dígitos do documento.</summary>
        string GerarHash(string documento);
    }
}
