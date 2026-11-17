using System.Net;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Web.Integracoes;
using Xunit;

namespace MechanicLtda.Web.Tests
{
    public class OrdemServicoHttpAppServiceTests
    {
        private const string OrdemJson = """
            {"id":7,"correlationId":"6d1f8a8e-1f0c-4c1a-9b7e-0a0a0a0a0a0a","dataCriacao":"2026-11-13T10:00:00Z",
             "status":7,"statusDescricao":"Aguardando Orçamento","descricaoProblema":"Barulho","valorTotalEstimado":500,
             "valorTotalItens":0,"veiculoId":1,"clienteId":2,"orcamentoId":1007,"pagamentoId":null,"itens":[]}
            """;

        [Fact]
        public async Task ObterPorId_mapeia_a_OS_do_servico_incluindo_os_status_novos()
        {
            var handler = new HandlerFalso().Responder(HttpStatusCode.OK, OrdemJson);
            var app = new OrdemServicoHttpAppService(handler.Cliente());

            var resposta = await app.ObterPorIdAsync("7");

            Assert.False(resposta.hasErrors);
            Assert.Equal(StatusOrdemServico.AguardandoOrcamento, resposta.getResponse.Status);
            Assert.Equal("Aguardando Orçamento", resposta.getResponse.StatusDescricao);
            Assert.Equal(1007, resposta.getResponse.OrcamentoId);
            Assert.Equal(2, resposta.getResponse.ClienteId);
            Assert.Equal("/api/ordemservico/7", handler.Chamadas.Single().Url);
        }

        [Fact]
        public async Task Erro_do_servico_vira_mensagem_no_ResponseDto()
        {
            var handler = new HandlerFalso().Responder(HttpStatusCode.Conflict,
                """{"success":false,"errors":["Com a Saga ligada, a aprovação é feita pelo pagamento."]}""");
            var app = new OrdemServicoHttpAppService(handler.Cliente());

            var resposta = await app.AprovarAsync("7");

            Assert.True(resposta.hasErrors);
            Assert.Contains("Com a Saga ligada, a aprovação é feita pelo pagamento.", resposta.getErrors);
            Assert.Equal(HttpMethod.Patch, handler.Chamadas.Single().Metodo);
            Assert.Equal("/api/ordemservico/7/aprovar", handler.Chamadas.Single().Url);
        }

        [Fact]
        public async Task Servico_fora_do_ar_nao_derruba_a_tela()
        {
            var handler = new HandlerFalso().Falhar(new HttpRequestException("connection refused"));
            var app = new OrdemServicoHttpAppService(handler.Cliente());

            var resposta = await app.ObterTodosAsync();

            Assert.True(resposta.hasErrors);
            Assert.Contains("indisponível", resposta.getErrors.Single());
        }

        [Fact]
        public async Task Atualizar_recusa_troca_de_cliente_sem_chamar_o_PUT()
        {
            var handler = new HandlerFalso().Responder(HttpStatusCode.OK, OrdemJson);
            var app = new OrdemServicoHttpAppService(handler.Cliente());

            var resposta = await app.AtualizarAsync("7", new OrdemServicoUpdateDto
            {
                DescricaoProblema = "Outro",
                ClienteId = 99,
                VeiculoId = 1
            });

            Assert.True(resposta.hasErrors);
            Assert.Single(handler.Chamadas);
        }

        [Fact]
        public async Task Atualizar_envia_so_descricao_e_valor()
        {
            var handler = new HandlerFalso()
                .Responder(HttpStatusCode.OK, OrdemJson)
                .Responder(HttpStatusCode.OK, OrdemJson);
            var app = new OrdemServicoHttpAppService(handler.Cliente());

            var resposta = await app.AtualizarAsync("7", new OrdemServicoUpdateDto
            {
                DescricaoProblema = "Outro",
                ValorTotalEstimado = 300,
                ClienteId = 2,
                VeiculoId = 1
            });

            Assert.False(resposta.hasErrors);
            var put = handler.Chamadas[1];
            Assert.Equal(HttpMethod.Put, put.Metodo);
            Assert.Contains("\"descricaoProblema\":\"Outro\"", put.Corpo);
            Assert.DoesNotContain("clienteId", put.Corpo);
        }

        [Fact]
        public async Task Cancelar_e_historico_usam_as_rotas_do_servico()
        {
            var handler = new HandlerFalso()
                .Responder(HttpStatusCode.OK, OrdemJson)
                .Responder(HttpStatusCode.OK, """
                    [{"statusAnterior":null,"statusAnteriorDescricao":null,"statusNovo":1,"statusNovoDescricao":"Recebida",
                      "dataAlteracao":"2026-11-13T10:00:00Z","origem":"Usuario","motivo":"Abertura da OS"}]
                    """);
            var app = new OrdemServicoHttpAppService(handler.Cliente());

            await app.CancelarAsync(7, "Cliente desistiu");
            var historico = await app.ObterHistoricoAsync(7);

            Assert.Equal("/api/ordemservico/7/cancelar", handler.Chamadas[0].Url);
            Assert.Contains("Cliente desistiu", handler.Chamadas[0].Corpo);
            Assert.Equal("Usuario", historico.getResponse.Single().Origem);
        }
    }
}
