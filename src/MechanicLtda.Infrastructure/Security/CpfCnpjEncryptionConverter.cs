using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Security.Cryptography;
using System.Text;

namespace MechanicLtda.Infrastructure.Security
{
    public class CpfCnpjEncryptionConverter : ValueConverter<string, string>
    {
        public CpfCnpjEncryptionConverter(string key) : base(
            v => Encrypt(v, key),
            v => Decrypt(v, key))
        { }

        private static string Encrypt(string plainText, string key)
        {
            using var aes = Aes.Create();
            aes.Key = DeriveKey(key);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        private static string Decrypt(string cipherTextBase64, string key)
        {
            try
            {
                var fullCipher = Convert.FromBase64String(cipherTextBase64);
                if (fullCipher.Length < 16)
                    return cipherTextBase64;

                using var aes = Aes.Create();
                aes.Key = DeriveKey(key);

                var iv = new byte[16];
                var cipherBytes = new byte[fullCipher.Length - 16];
                Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
                Buffer.BlockCopy(fullCipher, 16, cipherBytes, 0, cipherBytes.Length);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                // Dado gravado antes da criptografia entrar em vigor (ou corrompido por
                // truncamento da coluna, que era nvarchar(14) antes da migration
                // EncryptCpfCnpjCliente) não é um ciphertext válido para esta chave.
                // Sem esse fallback, uma única linha legada derruba toda a listagem
                // com "Padding is invalid and cannot be removed." em vez de exibir o
                // valor como veio do banco.
                return cipherTextBase64;
            }
        }

        private static byte[] DeriveKey(string key)
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(key));
        }
    }
}