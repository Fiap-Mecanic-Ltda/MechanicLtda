using System.Net;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Web.Integracoes;
using MechanicLtda.Web.ViewModels;
using Xunit;

namespace MechanicLtda.Web.Tests
{
    public class ExecucaoHttpTests
    {
        [Fact]
        public async Task Estoque_le_a_reserva_e_o_tipo_como_texto()
        {
            var handler = new HandlerFalso().Responder(HttpStatusCode.OK, """
                [{"id":5,"nome":"Correia","tipo":"Peca","quantidadeAtual":8,"quantidadeReservada":1,"quantidadeDisponivel":7,
                  "quantidadeMinima":2,"baixoEstoque":false,"dataUltimaAtualizacao":"2026-11-13T10:00:00Z"}]
                """);

            var resposta = await new EstoqueHttpAppService(handler.Cliente()).ObterTodosAsync();

            var item = resposta.getResponse.Single();
            Assert.Equal(TipoEstoque.Peca, item.Tipo);
            Assert.Equal(1, item.QuantidadeReservada);
            Assert.Equal(7, item.QuantidadeDisponivel);
        }

        [Fact]
        public async Task Reposicao_manda_o_campo_que_a_Execucao_espera()
        {
            var handler = new HandlerFalso().Responder(HttpStatusCode.OK, """{"id":5,"nome":"Correia","tipo":"Peca","quantidadeAtual":10}""");

            await new EstoqueHttpAppService(handler.Cliente()).ReporQuantidadeAsync("5", new EstoqueReposicaoDto { QuantidadeEntrada = 2 });

            Assert.Equal("/api/estoque/5/reposicao", handler.Chamadas.Single().Url);
            Assert.Contains("\"quantidade\":2", handler.Chamadas.Single().Corpo);
        }

        [Fact]
        public async Task Servicos_ativos_e_remocao()
        {
            var handler = new HandlerFalso()
                .Responder(HttpStatusCode.OK, """[{"id":1,"nome":"Troca","descricao":null,"valorBase":180,"ativo":true,"dataCadastro":"2026-11-13T10:00:00Z"}]""")
                .Responder(HttpStatusCode.NoContent);
            var app = new ServicoOficinaHttpAppService(handler.Cliente());

            var ativos = await app.ObterAtivosAsync();
            var removido = await app.RemoverAsync("1");

            Assert.Equal(180, ativos.getResponse.Single().ValorBase);
            Assert.True(removido.getResponse);
            Assert.Equal("/api/servicos-oficina/ativos", handler.Chamadas[0].Url);
        }

        [Fact]
        public async Task Fila_filtra_por_status_e_validacao_vira_mensagem()
        {
            var handler = new HandlerFalso()
                .Responder(HttpStatusCode.OK, """[{"ordemServicoId":2,"status":"NaFila","descricaoProblema":"x","dataEntrada":"2026-11-13T10:00:00Z","dataAlteracaoStatus":"2026-11-13T10:00:00Z"}]""")
                .Responder(HttpStatusCode.BadRequest, """{"title":"One or more validation errors occurred.","errors":{"Itens":["The Itens field is required."]}}""");
            var cliente = new ExecucaoHttpCliente(handler.Cliente());

            var fila = await cliente.FilaAsync("NaFila");
            var diagnostico = await cliente.RegistrarDiagnosticoAsync(2, [], null);

            Assert.Equal("/api/execucao/fila?status=NaFila", handler.Chamadas[0].Url);
            Assert.Equal("NaFila", fila.getResponse.Single().Status);
            Assert.Contains("The Itens field is required.", diagnostico.getErrors);
        }

        [Fact]
        public async Task Sessao_expirada_pede_novo_login()
        {
            var handler = new HandlerFalso().Responder(HttpStatusCode.Unauthorized);

            var resposta = await new ExecucaoHttpCliente(handler.Cliente()).ObterAsync(2);

            Assert.Contains("Saia e entre de novo", resposta.getErrors.Single());
        }

        [Fact]
        public void Linha_do_diagnostico_classifica_estoque_e_servico()
        {
            var tipos = new Dictionary<int, TipoEstoque> { [5] = TipoEstoque.Peca, [6] = TipoEstoque.Insumo };

            var peca = new LinhaDiagnosticoViewModel { Origem = "E:5", Quantidade = 1, ValorUnitario = 320 }.Converter(tipos);
            var insumo = new LinhaDiagnosticoViewModel { Origem = "E:6", Quantidade = 2 }.Converter(tipos);
            var servico = new LinhaDiagnosticoViewModel { Origem = "S:2", Descricao = " dianteiro ", Quantidade = 1 }.Converter(tipos);

            Assert.Equal(("Peca", 5), (peca!.Tipo, peca.EstoqueId!.Value));
            Assert.Equal("Insumo", insumo!.Tipo);
            Assert.Equal(("Servico", 2, "dianteiro"), (servico!.Tipo, servico.ServicoOficinaId!.Value, servico.Descricao));
        }

        [Theory]
        [InlineData("E:99")]
        [InlineData("X:1")]
        [InlineData("S:abc")]
        public void Linha_com_origem_invalida_nao_converte(string origem)
        {
            var linha = new LinhaDiagnosticoViewModel { Origem = origem, Quantidade = 1 };

            Assert.Null(linha.Converter(new Dictionary<int, TipoEstoque>()));
            Assert.False(linha.Vazia);
            Assert.True(new LinhaDiagnosticoViewModel { Descricao = "só texto" }.Vazia);
        }
    }
}
