using MechanicLtda.Application.AppServices;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Domain.Interfaces.Services;
using MechanicLtda.Domain.Services;
using MechanicLtda.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace MechanicLtda.Bootstrap.Extensions
{
    public static class DependencyInjectionExtension
    {
        public static IServiceCollection AddDependencyInjection(this IServiceCollection services)
        {
            // Notificador — Scoped para que as notificações vivam por requisição
            services.AddScoped<INotificadorService, NotificadorService>();

            // Repositórios
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IClienteRepository, ClienteRepository>();
            services.AddScoped<IVeiculoRepository, VeiculoRepository>();
            services.AddScoped<IOrdemServicoRepository, OrdemServicoRepository>();
            services.AddScoped<IItemOrdemServicoRepository, ItemOrdemServicoRepository>();
            services.AddScoped<IEstoqueRepository, EstoqueRepository>();
            services.AddScoped<IServicoOficinaRepository, ServicoOficinaRepository>();
            services.AddScoped<IOrcamentoRepository, OrcamentoRepository>();

            // Domain Services
            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<IClienteService, ClienteService>();
            services.AddScoped<IVeiculoService, VeiculoService>();
            services.AddScoped<IOrdemServicoService, OrdemServicoService>();
            services.AddScoped<IItemOrdemServicoService, ItemOrdemServicoService>();
            services.AddScoped<IEstoqueService, EstoqueService>();
            services.AddScoped<IServicoOficinaService, ServicoOficinaService>();
            services.AddScoped<IOrcamentoService, OrcamentoService>();

            // App Services
            services.AddScoped<IAuthAppService, AuthAppService>();
            services.AddScoped<IUsuarioAppService, UsuarioAppService>();
            services.AddScoped<IClienteAppService, ClienteAppService>();
            services.AddScoped<IVeiculoAppService, VeiculoAppService>();
            services.AddScoped<IOrdemServicoAppService, OrdemServicoAppService>();
            services.AddScoped<IItemOrdemServicoAppService, ItemOrdemServicoAppService>();
            services.AddScoped<IEstoqueAppService, EstoqueAppService>();
            services.AddScoped<IServicoOficinaAppService, ServicoOficinaAppService>();
            services.AddScoped<IOrcamentoAppService, OrcamentoAppService>();
            services.AddScoped<IOrcamentoPdfAppService, OrcamentoPdfAppService>();

            return services;
        }
    }
}
