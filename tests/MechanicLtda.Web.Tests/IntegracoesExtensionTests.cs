using System.Security.Claims;
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
        private static readonly Dictionary<string, string?> Enderecos = new()
        {
            ["Integracoes:OsService:BaseUrl"] = "http://os:8080",
            ["Integracoes:ExecucaoService:BaseUrl"] = "http://execucao:8080",
        };

        private static ServiceProvider Montar(Dictionary<string, string?> config)
        {
            var configuracao = new ConfigurationBuilder().AddInMemoryCollection(config).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddIntegracoesMicrosservicos(configuracao);
            return services.BuildServiceProvider();
        }

        [Fact]
        public void As_telas_usam_os_servicos()
        {
            using var sp = Montar(Enderecos);
            using var escopo = sp.CreateScope();
            var s = escopo.ServiceProvider;

            Assert.IsType<OrdemServicoHttpAppService>(s.GetRequiredService<IOrdemServicoCliente>());
            Assert.IsType<ItensOrdemServicoHttp>(s.GetRequiredService<IItensOrdemServicoCliente>());
            Assert.IsType<EstoqueHttpAppService>(s.GetRequiredService<IEstoqueCliente>());
            Assert.IsType<ServicoOficinaHttpAppService>(s.GetRequiredService<IServicoOficinaCliente>());
            Assert.IsType<ExecucaoHttpCliente>(s.GetRequiredService<IExecucaoCliente>());
        }

        [Theory]
        [InlineData("Integracoes:OsService:BaseUrl")]
        [InlineData("Integracoes:ExecucaoService:BaseUrl")]
        public void Sem_a_URL_de_um_servico_o_Web_nao_sobe(string chave)
        {
            var config = new Dictionary<string, string?>(Enderecos) { [chave] = null };
            Assert.Throws<InvalidOperationException>(() => Montar(config));
        }

        [Fact]
        public void Saga_ligada_por_padrao()
        {
            using var sp = Montar(Enderecos);
            Assert.True(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<IntegracoesOptions>>().Value.SagaHabilitada);
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
