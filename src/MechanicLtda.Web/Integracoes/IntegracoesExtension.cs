namespace MechanicLtda.Web.Integracoes
{
    public static class IntegracoesExtension
    {
        /// <summary>
        /// Registra os clientes HTTP do OrdemServico-service e do ExecucaoProducao-service.
        /// Sem as duas BaseUrl válidas o Web não sobe: não há mais OS nem estoque em processo.
        /// </summary>
        public static IServiceCollection AddIntegracoesMicrosservicos(this IServiceCollection services, IConfiguration configuration)
        {
            var opcoes = configuration.GetSection(IntegracoesOptions.Secao).Get<IntegracoesOptions>() ?? new IntegracoesOptions();
            Validar(opcoes.OsService, "OsService");
            Validar(opcoes.ExecucaoService, "ExecucaoService");
            services.Configure<IntegracoesOptions>(configuration.GetSection(IntegracoesOptions.Secao));

            services.AddHttpContextAccessor();
            services.AddTransient<TokenDoUsuarioHandler>();

            AddCliente<IEstoqueCliente, EstoqueHttpAppService>(services, opcoes.ExecucaoService);
            AddCliente<IServicoOficinaCliente, ServicoOficinaHttpAppService>(services, opcoes.ExecucaoService);
            AddCliente<IExecucaoCliente, ExecucaoHttpCliente>(services, opcoes.ExecucaoService);
            AddCliente<IOrdemServicoCliente, OrdemServicoHttpAppService>(services, opcoes.OsService);
            AddCliente<IItensOrdemServicoCliente, ItensOrdemServicoHttp>(services, opcoes.OsService);

            return services;
        }

        private static void AddCliente<TInterface, TImplementacao>(IServiceCollection services, ServicoRemotoOptions opcoes)
            where TInterface : class
            where TImplementacao : class, TInterface
        {
            services.AddHttpClient<TInterface, TImplementacao>(http =>
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
                    $"Integracoes:{nome}:BaseUrl precisa ser a URL do serviço (ex.: http://{nome.ToLowerInvariant()}:8080).");
        }
    }
}
