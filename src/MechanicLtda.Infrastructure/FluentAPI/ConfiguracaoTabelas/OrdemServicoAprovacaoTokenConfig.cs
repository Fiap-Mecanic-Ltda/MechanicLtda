using MechanicLtda.Domain.Entities;
using MechanicLtda.Infrastructure.FluentAPI.ConfiguracaoTabelas.Base;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Infrastructure.FluentAPI.ConfiguracaoTabelas
{
    public class OrdemServicoAprovacaoTokenConfig : ConfiguracaoContextoBase
    {
        public override void Configurar(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrdemServicoAprovacaoToken>(entity =>
            {
                entity.ToTable("OrdemServicoAprovacaoTokens");

                entity.Property(e => e.Token)
                      .IsRequired()
                      .HasMaxLength(64);

                entity.HasIndex(e => e.Token)
                      .IsUnique();

                entity.HasOne(e => e.OrdemServico)
                      .WithMany()
                      .HasForeignKey(e => e.OrdemServicoId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
