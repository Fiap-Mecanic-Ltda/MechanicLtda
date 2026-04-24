using MechanicLtda.Domain.Entities;
using MechanicLtda.Infrastructure.FluentAPI.ConfiguracaoTabelas.Base;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace MechanicLtda.Infrastructure
{
    public class BancoAPIContext : IdentityDbContext<Usuario>
    {
        public BancoAPIContext(DbContextOptions<BancoAPIContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Veiculo> Veiculos { get; set; }
        public DbSet<OrdemServico> OrdensServico { get; set; }
        public DbSet<ItemOrdemServico> ItensOrdemServico { get; set; }
        public DbSet<Estoque> Estoques { get; set; }
        public DbSet<Orcamento> Orcamentos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BancoAPIContext).Assembly);

            ConfigurarEntidadesAtravesDasClassesConfig(modelBuilder);
        }

        private void ConfigurarEntidadesAtravesDasClassesConfig(ModelBuilder modelBuilder)
        {
            var configTypes = RecuperarTodosOsTiposQueHerdamDaClasse(typeof(ConfiguracaoContextoBase));

            foreach (var configType in configTypes)
            {
                var obj = (ConfiguracaoContextoBase)Activator.CreateInstance(configType);
                obj.Configurar(modelBuilder);
            }
        }

        IEnumerable<Type> RecuperarTodosOsTiposQueHerdamDaClasse(Type MyType)
        {
            return Assembly.GetAssembly(MyType)
            .GetTypes()
            .Where(TheType =>
            TheType.IsClass
            && !TheType.IsAbstract
            && TheType.IsSubclassOf(MyType)
            );
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries().Where(entry => entry.Entity.GetType().GetProperty("DataCadastro") != null))
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property("DataCadastro").CurrentValue = DateTime.Now;
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Property("DataCadastro").IsModified = false;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}