using MechanicLtda.Domain.Entities;
using MechanicLtda.Infrastructure.FluentAPI.ConfiguracaoTabelas.Base;
using MechanicLtda.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Infrastructure.FluentAPI.ConfiguracaoTabelas
{
    public class ClienteConfig : ConfiguracaoContextoBase
    {
        private readonly string _encryptionKey;

        public ClienteConfig() : this(string.Empty) { }

        public ClienteConfig(string encryptionKey)
        {
            _encryptionKey = encryptionKey;
        }

        public override void Configurar(ModelBuilder modelBuilder)
        {
            if (string.IsNullOrEmpty(_encryptionKey))
                return;

            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.Property(c => c.CpfCnpj)
                    .HasMaxLength(500)
                    .HasConversion(new CpfCnpjEncryptionConverter(_encryptionKey));
            });
        }
    }
}