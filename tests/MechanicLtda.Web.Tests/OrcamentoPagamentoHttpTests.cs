using System.Net;
using System.Text.Json;
using MechanicLtda.Web.Integracoes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MechanicLtda.Web.Tests
{
    public class OrcamentoPagamentoHttpTests
    {
        private const string OrcamentoJson = """
            {"hasErrors":false,"getErrors":[],"getResponse":{"id":12,"ordemServicoId":7,"valorTotalPecas":300,
             "valorTotalInsumos":50,"valorTotalServicos":100,"valorTotalGeral":450,"dataGeracao":"2026-11-16T10:00:00Z",
             "validade":null,"ajustadoManualmente":false,"status":1,"statusDescricao":"Aberto","motivoStatus":null,
             "correlationId":"6d1f8a8e-1f0c-4c1a-9b7e-0a0a0a0a0a0a",
             "itens":[{"id":1,"itemOrigemId":3,"descricao":"Pastilha","categoria":1,"quantidade":2,"valorUnitario":150,"valorTotal":300}]}}
            """;

        private const string PagamentoJson = """
            {"hasErrors":false,"getErrors":[],"getResponse":{"id":5,"requisicaoId":"3f6a1c2e-9b7d-4a11-8f2e-5c0d7b9a1e44",
             "orcamentoId":12,"ordemServicoId":7,"valor":450,"status":4,"statusDescricao":"Aprovado","statusDetalhe":"accredited",
             "provedor":"MercadoPago","idExterno":"123","parcelas":1,"metodoPagamento":"master","modoTeste":true,
             "criadoEm":"2026-11-16T10:00:00Z","atualizadoEm":"2026-11-16T10:00:01Z"}}
            """;

        [Fact]
        public async Task Le_o_orcamento_da_OS_no_envelope_ResponseDto()
        {
            var handler = new HandlerFalso().Responder(HttpStatusCode.OK, OrcamentoJson);
            var cliente = new OrcamentoHttpCliente(handler.Cliente());

            var resposta = await cliente.ObterPorOrdemServicoAsync(7);

            Assert.False(resposta.hasErrors);
            var o = resposta.getResponse!;
            Assert.Equal(12, o.Id);
            Assert.Equal(450m, o.ValorTotalGeral);
            Assert.Equal(StatusOrcamento.Aberto, o.Status);
            Assert.True(o.AceitaDecisao);
            Assert.Equal(CategoriaItemOrcamento.Peca, o.Itens.Single().Categoria);
            Assert.Equal("/api/orcamento/ordem-servico/7", handler.Chamadas.Single().Url);
        }

        [Fact]
        public async Task Orcamento_ainda_nao_gerado_nao_e_erro()
        {
            var handler = new HandlerFalso().Responder(HttpStatusCode.NotFound,
                """{"hasErrors":true,"getErrors":["Nenhum orcamento encontrado para a OS 7."],"getResponse":null}""");
            var cliente = new OrcamentoHttpCliente(handler.Cliente());

            var resposta = await cliente.ObterPorOrdemServicoAsync(7);

            Assert.False(resposta.hasErrors);
            Assert.Null(resposta.getResponse);
        }

        [Fact]
        public async Task Recusa_manda_o_motivo_e_409_vira_a_mensagem_do_servico()
        {
            var handler = new HandlerFalso().Responder(HttpStatusCode.Conflict,
                """{"hasErrors":true,"getErrors":["O orcamento ja foi pago."],"getResponse":null}""");
            var cliente = new OrcamentoHttpCliente(handler.Cliente());

            var resposta = await cliente.RecusarAsync(12, "  caro demais ");

            Assert.True(resposta.hasErrors);
            Assert.Contains("O orcamento ja foi pago.", resposta.getErrors);
            var chamada = handler.Chamadas.Single();
            Assert.Equal(HttpMethod.Patch, chamada.Metodo);
            Assert.Equal("/api/orcamento/12/recusar", chamada.Url);
            Assert.Equal("caro demais", JsonDocument.Parse(chamada.Corpo!).RootElement.GetProperty("motivo").GetString());
        }

        [Fact]
        public async Task Aprovar_usa_PATCH_sem_corpo()
        {
            var handler = new HandlerFalso().Responder(HttpStatusCode.OK, OrcamentoJson.Replace("\"status\":1", "\"status\":2"));
            var cliente = new OrcamentoHttpCliente(handler.Cliente());

            var resposta = await cliente.AprovarAsync(12);

            Assert.Equal(StatusOrcamento.Aprovado, resposta.getResponse.Status);
            Assert.Equal(HttpMethod.Patch, handler.Chamadas.Single().Metodo);
            Assert.Equal("/api/orcamento/12/aprovar", handler.Chamadas.Single().Url);
        }

        [Fact]
        public async Task Pdf_volta_os_bytes()
        {
            var handler = new HandlerFalso().Responder(HttpStatusCode.OK, "%PDF-1.7");
            var cliente = new OrcamentoHttpCliente(handler.Cliente());

            var resposta = await cliente.ObterPdfAsync(12);

            Assert.False(resposta.hasErrors);
            Assert.Equal("%PDF-1.7", System.Text.Encoding.UTF8.GetString(resposta.getResponse));
            Assert.Equal("/api/orcamento/12/pdf", handler.Chamadas.Single().Url);
        }

        [Fact]
        public async Task Pagamento_manda_token_e_requisicao_sem_valor()
        {
            var handler = new HandlerFalso().Responder(HttpStatusCode.Created, PagamentoJson);
            var cliente = new PagamentoHttpCliente(handler.Cliente());
            var requisicao = Guid.NewGuid();

            var resposta = await cliente.CriarAsync(new PagamentoCreateDto
            {
                RequisicaoId = requisicao,
                OrcamentoId = 12,
                Token = "tok",
                PaymentMethodId = "master",
                Installments = 1,
                Payer = new PagadorDto { Email = "cliente@teste.com", Identification = new IdentificacaoDto("CPF", "12345678909") }
            });

            Assert.False(resposta.hasErrors);
            Assert.Equal(StatusPagamento.Aprovado, resposta.getResponse.Status);
            var chamada = handler.Chamadas.Single();
            Assert.Equal("/api/pagamento", chamada.Url);
            var corpo = JsonDocument.Parse(chamada.Corpo!).RootElement;
            Assert.Equal(requisicao, corpo.GetProperty("requisicaoId").GetGuid());
            Assert.Equal("tok", corpo.GetProperty("token").GetString());
            Assert.Equal("12345678909", corpo.GetProperty("payer").GetProperty("identification").GetProperty("number").GetString());
            Assert.False(corpo.TryGetProperty("valor", out _));
        }

        [Fact]
        public async Task Erro_de_validacao_do_pagamento_vira_mensagem()
        {
            var handler = new HandlerFalso().Responder(HttpStatusCode.BadRequest,
                """{"title":"One or more validation errors occurred.","errors":{"Token":["O token do cartão é obrigatório."]}}""");
            var cliente = new PagamentoHttpCliente(handler.Cliente());

            var resposta = await cliente.CriarAsync(new PagamentoCreateDto { OrcamentoId = 12 });

            Assert.Contains("O token do cartão é obrigatório.", resposta.getErrors);
        }

        [Fact]
        public async Task Sincronizar_sem_mudanca_nao_e_erro()
        {
            var handler = new HandlerFalso().Responder(HttpStatusCode.NoContent);
            var cliente = new PagamentoHttpCliente(handler.Cliente());

            var resposta = await cliente.SincronizarAsync(5);

            Assert.False(resposta.hasErrors);
            Assert.Null(resposta.getResponse);
            Assert.Equal("/api/pagamento/5/sincronizar", handler.Chamadas.Single().Url);
        }

        [Fact]
        public async Task Lista_os_pagamentos_do_orcamento()
        {
            var lista = $$"""{"hasErrors":false,"getErrors":[],"getResponse":[{{JsonDocument.Parse(PagamentoJson).RootElement.GetProperty("getResponse").GetRawText()}}]}""";
            var handler = new HandlerFalso().Responder(HttpStatusCode.OK, lista);
            var cliente = new PagamentoHttpCliente(handler.Cliente());

            var resposta = await cliente.ListarPorOrcamentoAsync(12);

            Assert.Single(resposta.getResponse);
            Assert.Equal("/api/pagamento/orcamento/12", handler.Chamadas.Single().Url);
        }

        [Fact]
        public async Task Sem_a_URL_do_servico_o_Web_sobe_e_avisa()
        {
            var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Integracoes:OsService:BaseUrl"] = "http://os:8080",
                ["Integracoes:ExecucaoService:BaseUrl"] = "http://execucao:8080",
            }).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddIntegracoesMicrosservicos(configuracao);
            using var sp = services.BuildServiceProvider();

            var resposta = await sp.GetRequiredService<IOrcamentoCliente>().ObterPorOrdemServicoAsync(7);

            Assert.True(resposta.hasErrors);
            Assert.Contains("não está configurado", resposta.getErrors.Single());
            Assert.IsType<OrcamentoPagamentoNaoConfigurado>(sp.GetRequiredService<IPagamentoCliente>());
        }

        [Fact]
        public void Com_a_URL_usa_os_clientes_HTTP()
        {
            var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Integracoes:OsService:BaseUrl"] = "http://os:8080",
                ["Integracoes:ExecucaoService:BaseUrl"] = "http://execucao:8080",
                ["Integracoes:OrcamentoService:BaseUrl"] = "https://gateway.exemplo/",
            }).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddIntegracoesMicrosservicos(configuracao);
            using var sp = services.BuildServiceProvider();
            using var escopo = sp.CreateScope();

            Assert.IsType<OrcamentoHttpCliente>(escopo.ServiceProvider.GetRequiredService<IOrcamentoCliente>());
            Assert.IsType<PagamentoHttpCliente>(escopo.ServiceProvider.GetRequiredService<IPagamentoCliente>());
        }
    }
}
