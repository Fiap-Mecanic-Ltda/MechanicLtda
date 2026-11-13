using MechanicLtda.Application.AppServices.Interfaces;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace MechanicLtda.Web.Integracoes
{
    public static class IntegracoesExtension
    {
        /// <summary>
        /// Registra os clientes HTTP dos microsservicos e, para cada chave ligada, troca a
        /// implementação em processo pela remota. Precisa vir depois de AddDependencyInjection.
        /// </summary>
        public static IServiceCollection AddIntegracoesMicrosservicos(this IServiceCollection services, IConfiguration configuration)
        {
            var opcoes = configuration.GetSection(IntegracoesOptions.Secao).Get<IntegracoesOptions>() ?? new IntegracoesOptions();
            services.Configure<IntegracoesOptions>(configuration.GetSection(IntegracoesOptions.Secao));

            services.AddHttpContextAccessor();
            services.AddTransient<TokenDoUsuarioHandler>();

            if (opcoes.ExecucaoService.Habilitado)
            {
                Validar(opcoes.ExecucaoService, "ExecucaoService");
                AddCliente<EstoqueHttpAppService>(services, opcoes.ExecucaoService);
                AddCliente<ServicoOficinaHttpAppService>(services, opcoes.ExecucaoService);
                AddCliente<ExecucaoHttpCliente>(services, opcoes.ExecucaoService);

                services.Replace(ServiceDescriptor.Scoped<IEstoqueAppService>(sp => sp.GetRequiredService<EstoqueHttpAppService>()));
                services.Replace(ServiceDescriptor.Scoped<IServicoOficinaAppService>(sp => sp.GetRequiredService<ServicoOficinaHttpAppService>()));
                services.AddScoped<IExecucaoCliente>(sp => sp.GetRequiredService<ExecucaoHttpCliente>());
            }

            if (opcoes.OsService.Habilitado)
            {
                Validar(opcoes.OsService, "OsService");
                AddCliente<OrdemServicoHttpAppService>(services, opcoes.OsService);
                AddCliente<ItensOrdemServicoHttp>(services, opcoes.OsService);

                services.Replace(ServiceDescriptor.Scoped<IOrdemServicoAppService>(sp => sp.GetRequiredService<OrdemServicoHttpAppService>()));
                services.AddScoped<IOrdemServicoRemoto>(sp => sp.GetRequiredService<OrdemServicoHttpAppService>());
                services.AddScoped<IItensOrdemServicoWeb>(sp => sp.GetRequiredService<ItensOrdemServicoHttp>());
            }
            else
            {
                services.AddScoped<IItensOrdemServicoWeb, ItensOrdemServicoEmProcesso>();
            }

            return services;
        }

        private static void AddCliente<T>(IServiceCollection services, ServicoRemotoOptions opcoes) where T : class
        {
            services.AddHttpClient<T>(http =>
                {
                    http.BaseAddress = new Uri(opcoes.BaseUrl.TrimEnd('/') + "/");
                    http.Timeout = TimeSpan.FromSeconds(opcoes.TimeoutSegundos);
                })
                .AddHttpMessageHandler<TokenDoUsuarioHandler>();
        }

        private static void Validar(ServicoRemotoOptions opcoes, string nome)
        {
            if (!Uri.TryCreate(opcoes.BaseUrl, UriKind.Absolute, out _))
                throw new InvalidOperationException(
                    $"Integracoes:{nome}:Habilitado está ligado, mas Integracoes:{nome}:BaseUrl não é uma URL válida.");
        }
    }

    /// <summary>O que as views precisam saber sobre as chaves.</summary>
    public static class IntegracoesViewExtensions
    {
        public static IntegracoesOptions Integracoes(this IServiceProvider services) =>
            services.GetRequiredService<IOptions<IntegracoesOptions>>().Value;
    }
}
