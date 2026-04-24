using MechanicLtda.Domain.Entities;
using MechanicLtda.Infrastructure.FluentAPI.ConfiguracaoTabelas.Base;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Infrastructure.FluentAPI.ConfiguracaoTabelas
{
    public class OrcamentoConfig : ConfiguracaoContextoBase
    {
        public override void Configurar(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Orcamento>(entity =>
            {
                entity.ToTable("Orcamentos");

                entity.Property(e => e.ValorTotalPecas)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)");

                entity.Property(e => e.ValorTotalInsumos)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)");

                entity.Property(e => e.ValorTotalGeral)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)");

                entity.Property(e => e.DataGeracao)
                      .IsRequired();

                entity.HasOne(e => e.OrdemServico)
                      .WithOne(o => o.Orcamento)
                      .HasForeignKey<Orcamento>(e => e.OrdemServicoId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.OrdemServicoId)
                      .IsUnique();
            });
        }
    }
}