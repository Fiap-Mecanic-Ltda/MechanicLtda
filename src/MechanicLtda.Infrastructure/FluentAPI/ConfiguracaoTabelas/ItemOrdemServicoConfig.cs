using MechanicLtda.Domain.Entities;
using MechanicLtda.Infrastructure.FluentAPI.ConfiguracaoTabelas.Base;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Infrastructure.FluentAPI.ConfiguracaoTabelas
{
    public class ItemOrdemServicoConfig : ConfiguracaoContextoBase
    {
        public override void Configurar(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ItemOrdemServico>(entity =>
            {
                entity.ToTable("ItensOrdemServico");

                entity.Property(e => e.Quantidade)
                      .IsRequired();

                entity.Property(e => e.ValorUnitario)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)");

                entity.Property(e => e.ValorTotal)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.OrdemServico)
                      .WithMany(o => o.ItensOrdemServico)
                      .HasForeignKey(e => e.OrdemServicoId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}