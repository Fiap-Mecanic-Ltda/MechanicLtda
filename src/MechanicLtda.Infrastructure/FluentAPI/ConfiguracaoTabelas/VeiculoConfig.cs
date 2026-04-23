using MechanicLtda.Domain.Entities;
using MechanicLtda.Infrastructure.FluentAPI.ConfiguracaoTabelas.Base;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Infrastructure.FluentAPI.ConfiguracaoTabelas
{
    public class VeiculoConfig : ConfiguracaoContextoBase
    {
        public override void Configurar(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Veiculo>(p =>
            {
                p.HasOne(p => p.Cliente).WithMany(c => c.Veiculos).HasForeignKey(p => p.ClienteId).IsRequired(true).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}