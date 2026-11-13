using System.Security.Claims;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Web.Integracoes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace MechanicLtda.Web.Tests
{
    public class IntegracoesExtensionTests
    {
        private static ServiceProvider Montar(Dictionary<string, string?> config)
        {
            var configuracao = new ConfigurationBuilder().AddInMemoryCollection(config).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            // O que o Bootstrap registra hoje (em processo).
            services.AddScoped(_ => Mock.Of<IOrdemServicoAppService>());
            services.AddScoped(_ => Mock.Of<IItemOrdemServicoAppService>());
            services.AddScoped(_ => Mock.Of<IEstoqueAppService>());
            services.AddScoped(_ => Mock.Of<IServicoOficinaAppService>());
            services.AddIntegracoesMicrosservicos(configuracao);
            return services.BuildServiceProvider();
        }

        [Fact]
        public void Sem_chaves_tudo_continua_em_processo()
        {
            using var sp = Montar([]);
            using var escopo = sp.CreateScope();

            Assert.IsNotType<OrdemServicoHttpAppService>(escopo.ServiceProvider.GetRequiredService<IOrdemServicoAppService>());
            Assert.IsType<ItensOrdemServicoEmProcesso>(escopo.ServiceProvider.GetRequiredService<IItensOrdemServicoWeb>());
            Assert.Null(escopo.ServiceProvider.GetService<IOrdemServicoRemoto>());
            Assert.Null(escopo.ServiceProvider.GetService<IExecucaoCliente>());
        }

        [Fact]
        public void Com_as_chaves_as_telas_usam_os_servicos()
        {
            using var sp = Montar(new()
            {
                ["Integracoes:OsService:Habilitado"] = "true",
                ["Integracoes:OsService:BaseUrl"] = "http://os:8080",
                ["Integracoes:ExecucaoService:Habilitado"] = "true",
                ["Integracoes:ExecucaoService:BaseUrl"] = "http://execucao:8080",
            });
            using var escopo = sp.CreateScope();
            var s = escopo.ServiceProvider;

            Assert.IsType<OrdemServicoHttpAppService>(s.GetRequiredService<IOrdemServicoAppService>());
            Assert.IsType<ItensOrdemServicoHttp>(s.GetRequiredService<IItensOrdemServicoWeb>());
            Assert.IsType<EstoqueHttpAppService>(s.GetRequiredService<IEstoqueAppService>());
            Assert.IsType<ServicoOficinaHttpAppService>(s.GetRequiredService<IServicoOficinaAppService>());
            Assert.IsType<ExecucaoHttpCliente>(s.GetRequiredService<IExecucaoCliente>());
            Assert.NotNull(s.GetService<IOrdemServicoRemoto>());
        }

        [Fact]
        public void Chave_ligada_sem_URL_falha_na_subida()
        {
            Assert.Throws<InvalidOperationException>(() => Montar(new() { ["Integracoes:OsService:Habilitado"] = "true" }));
        }

        [Fact]
        public async Task Handler_repassa_o_token_do_usuario_e_a_correlacao()
        {
            var contexto = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(TokenDoUsuarioHandler.ClaimToken, "jwt-do-usuario")], "Cookies"))
            };
            contexto.Request.Headers[TokenDoUsuarioHandler.CabecalhoCorrelacao] = "corr-123";
            var acessor = new Mock<IHttpContextAccessor>();
            acessor.Setup(a => a.HttpContext).Returns(contexto);

            HttpRequestMessage? enviada = null;
            var destino = new HandlerCaptura(r => enviada = r);
            using var http = new HttpClient(new TokenDoUsuarioHandler(acessor.Object) { InnerHandler = destino });

            await http.GetAsync("http://servico/api/ordemservico");

            Assert.Equal("Bearer jwt-do-usuario", enviada!.Headers.Authorization!.ToString());
            Assert.Equal("corr-123", enviada.Headers.GetValues(TokenDoUsuarioHandler.CabecalhoCorrelacao).Single());
        }

        private sealed class HandlerCaptura(Action<HttpRequestMessage> captura) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                captura(request);
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            }
        }
    }
}
