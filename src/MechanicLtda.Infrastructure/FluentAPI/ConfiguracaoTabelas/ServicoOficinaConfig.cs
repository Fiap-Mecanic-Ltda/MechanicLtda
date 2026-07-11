using MechanicLtda.Domain.Entities;
using MechanicLtda.Infrastructure.FluentAPI.ConfiguracaoTabelas.Base;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Infrastructure.FluentAPI.ConfiguracaoTabelas
{
    public class ServicoOficinaConfig : ConfiguracaoContextoBase
    {
        public override void Configurar(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ServicoOficina>(entity =>
            {
                entity.ToTable("ServicosOficina");

                entity.Property(e => e.Nome)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.Descricao)
                      .IsRequired()
                      .HasMaxLength(1000);

                entity.Property(e => e.ValorBase)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Ativo)
                      .IsRequired();

                entity.Property(e => e.DataCadastro)
                      .IsRequired();

                entity.Property(e => e.DataAtualizacao)
                      .IsRequired(false);

                entity.HasMany(e => e.ItensOrdemServico)
                      .WithOne(i => i.ServicoOficina)
                      .HasForeignKey(i => i.ServicoOficinaId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
