using MechanicLtda.Domain.Entities;
using MechanicLtda.Infrastructure.FluentAPI.ConfiguracaoTabelas.Base;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Infrastructure.FluentAPI.ConfiguracaoTabelas
{
    /// <summary>
    /// Configuração do índice cego do CPF/CNPJ. Fica separada de <see cref="ClienteConfig"/>
    /// porque aquela classe só é aplicada quando a chave de criptografia está presente,
    /// enquanto a coluna de hash faz parte do schema em qualquer ambiente.
    /// </summary>
    public class ClienteHashConfig : ConfiguracaoContextoBase
    {
        public override void Configurar(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.Property(c => c.CpfCnpjHash)
                      .HasMaxLength(64);

                // Índice filtrado: linhas antigas ficam com hash nulo até o backfill rodar,
                // e vários nulos não convivem num índice único do SQL Server.
                entity.HasIndex(c => c.CpfCnpjHash)
                      .IsUnique()
                      .HasFilter("[CpfCnpjHash] IS NOT NULL");
            });
        }
    }
}
