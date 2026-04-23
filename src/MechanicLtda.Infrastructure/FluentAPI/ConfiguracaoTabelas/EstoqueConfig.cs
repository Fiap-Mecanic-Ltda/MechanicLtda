using MechanicLtda.Domain.Entities;
using MechanicLtda.Infrastructure.FluentAPI.ConfiguracaoTabelas.Base;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Infrastructure.FluentAPI.ConfiguracaoTabelas
{
    public class EstoqueConfig : ConfiguracaoContextoBase
    {
        public override void Configurar(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Estoque>(entity =>
            {
                entity.ToTable("Estoques");

                entity.Property(e => e.Nome)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.Tipo)
                      .IsRequired();

                entity.Property(e => e.QuantidadeAtual)
                      .IsRequired();

                entity.Property(e => e.QuantidadeMinima)
                      .IsRequired();

                entity.Property(e => e.DataUltimaAtualizacao)
                      .IsRequired();

                entity.HasMany(e => e.ItensOrdemServico)
                      .WithOne(i => i.Estoque)
                      .HasForeignKey(i => i.EstoqueId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}