using MechanicLtda.Application.AppServices;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Domain.Interfaces.Services;
using MechanicLtda.Domain.Services;
using MechanicLtda.Infrastructure.Repositories;

namespace MechanicLtda.API.Extensions
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

            // Domain Services
            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<IClienteService, ClienteService>();
            services.AddScoped<IVeiculoService, VeiculoService>();
            services.AddScoped<IOrdemServicoService, OrdemServicoService>();
            services.AddScoped<IItemOrdemServicoService, ItemOrdemServicoService>();

            // App Services
            services.AddScoped<IAuthAppService, AuthAppService>();
            services.AddScoped<IUsuarioAppService, UsuarioAppService>();
            services.AddScoped<IClienteAppService, ClienteAppService>();
            services.AddScoped<IVeiculoAppService, VeiculoAppService>();
            services.AddScoped<IOrdemServicoAppService, OrdemServicoAppService>();
            services.AddScoped<IItemOrdemServicoAppService, ItemOrdemServicoAppService>();

            return services;
        }
    }
}