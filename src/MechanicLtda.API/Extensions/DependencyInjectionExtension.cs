using MechanicLtda.Application.AppServices;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Domain.Interfaces.Services;
using MechanicLtda.Domain.Services;
using MechanicLtda.Infrastructure.Repositories;
using MechanicLtda.Infrastructure.Services;

namespace MechanicLtda.API.Extensions
{
    public static class DependencyInjectionExtension
    {
        public static IServiceCollection AddDependencyInjection(this IServiceCollection services)
        {
            // Notificador � Scoped para que as notifica��es vivam por requisi��o
            services.AddScoped<INotificadorService, NotificadorService>();

            // Reposit�rios
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IClienteRepository, ClienteRepository>();
            services.AddScoped<IVeiculoRepository, VeiculoRepository>();
            services.AddScoped<IOrdemServicoRepository, OrdemServicoRepository>();
            services.AddScoped<IItemOrdemServicoRepository, ItemOrdemServicoRepository>();
            services.AddScoped<IEstoqueRepository, EstoqueRepository>();
            services.AddScoped<IOrcamentoRepository, OrcamentoRepository>();
            services.AddScoped<IOrdemServicoAprovacaoTokenRepository, OrdemServicoAprovacaoTokenRepository>();

            // Serviços de Infraestrutura
            services.AddScoped<IEmailService, EmailService>();

            // Domain Services
            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<IClienteService, ClienteService>();
            services.AddScoped<IVeiculoService, VeiculoService>();
            services.AddScoped<IOrdemServicoService, OrdemServicoService>();
            services.AddScoped<IItemOrdemServicoService, ItemOrdemServicoService>();
            services.AddScoped<IEstoqueService, EstoqueService>();
            services.AddScoped<IOrcamentoService, OrcamentoService>();

            // App Services
            services.AddScoped<IAuthAppService, AuthAppService>();
            services.AddScoped<IUsuarioAppService, UsuarioAppService>();
            services.AddScoped<IClienteAppService, ClienteAppService>();
            services.AddScoped<IVeiculoAppService, VeiculoAppService>();
            services.AddScoped<IOrdemServicoAppService, OrdemServicoAppService>();
            services.AddScoped<IItemOrdemServicoAppService, ItemOrdemServicoAppService>();
            services.AddScoped<IEstoqueAppService, EstoqueAppService>();
            services.AddScoped<IOrcamentoAppService, OrcamentoAppService>();

            return services;
        }
    }
}