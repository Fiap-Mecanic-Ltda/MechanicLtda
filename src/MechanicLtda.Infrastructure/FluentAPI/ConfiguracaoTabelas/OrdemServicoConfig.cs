using MechanicLtda.Domain.Entities;
using MechanicLtda.Infrastructure.FluentAPI.ConfiguracaoTabelas.Base;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Infrastructure.FluentAPI.ConfiguracaoTabelas
{
    public class OrdemServicoConfig : ConfiguracaoContextoBase
    {
        public override void Configurar(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrdemServico>(entity =>
            {
                entity.ToTable("OrdensServico");

                entity.Property(e => e.DescricaoProblema)
                      .IsRequired()
                      .HasMaxLength(1000);

                entity.Property(e => e.ValorTotalEstimado)
                      .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Status)
                      .IsRequired()
                      .HasConversion<int>();

                entity.HasOne(e => e.Veiculo)
                      .WithMany()
                      .HasForeignKey(e => e.VeiculoId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Cliente)
                      .WithMany()
                      .HasForeignKey(e => e.ClienteId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}