using System.Net;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Web.Integracoes;
using Moq;
using Xunit;

namespace MechanicLtda.Web.Tests
{
    public class ItensOrdemServicoHttpTests
    {
        private const string ItemJson = """
            {"id":3,"descricao":"Filtro de Ar","tipo":"Peca","quantidade":2,"valorUnitario":40,"valorTotal":80,"estoqueId":2,"servicoOficinaId":null}
            """;

        private readonly Mock<IEstoqueCliente> _estoque = new();
        private readonly Mock<IServicoOficinaCliente> _servicos = new();

        private ItensOrdemServicoHttp Criar(HandlerFalso handler) => new(handler.Cliente(), _estoque.Object, _servicos.Object);

        [Theory]
        [InlineData(TipoEstoque.Peca, "Peca")]
        [InlineData(TipoEstoque.Insumo, "Insumo")]
        public async Task Item_de_estoque_leva_o_tipo_do_cadastro(TipoEstoque tipo, string esperado)
        {
            _estoque.Setup(e => e.ObterPorIdAsync("2"))
                .ReturnsAsync(new ResponseDto<EstoqueDto>().setResponse(new EstoqueDto { Id = 2, Nome = "Filtro de Ar", Tipo = tipo }));
            var handler = new HandlerFalso().Responder(HttpStatusCode.Created, ItemJson);

            var resposta = await Criar(handler).AdicionarAsync(10, new ItemOrdemServicoCreateDto { EstoqueId = 2, Quantidade = 2, ValorUnitario = 40 });

            Assert.False(resposta.hasErrors);
            var chamada = handler.Chamadas.Single();
            Assert.Equal("/api/ordem-servico/10/itens", chamada.Url);
            Assert.Contains($"\"tipo\":\"{esperado}\"", chamada.Corpo);
            Assert.Contains("\"descricao\":\"Filtro de Ar\"", chamada.Corpo);
            Assert.Equal(10, resposta.getResponse.OrdemServicoId);
            Assert.Equal(80, resposta.getResponse.ValorTotal);
        }

        [Fact]
        public async Task Servico_do_catalogo_vira_Servico_com_o_nome_e_o_complemento()
        {
            _servicos.Setup(s => s.ObterPorIdAsync("4"))
                .ReturnsAsync(new ResponseDto<ServicoOficinaDto>().setResponse(new ServicoOficinaDto { Id = 4, Nome = "Alinhamento" }));
            var handler = new HandlerFalso().Responder(HttpStatusCode.Created, ItemJson);

            await Criar(handler).AdicionarAsync(10, new ItemOrdemServicoCreateDto { ServicoOficinaId = 4, DescricaoServico = "dianteiro", Quantidade = 1, ValorUnitario = 90 });

            var corpo = handler.Chamadas.Single().Corpo;
            Assert.Contains("\"tipo\":\"Servico\"", corpo);
            Assert.Contains("\"descricao\":\"Alinhamento - dianteiro\"", corpo);
        }

        [Fact]
        public async Task Na_edicao_a_descricao_que_ja_tem_o_nome_nao_repete()
        {
            _servicos.Setup(s => s.ObterPorIdAsync("4"))
                .ReturnsAsync(new ResponseDto<ServicoOficinaDto>().setResponse(new ServicoOficinaDto { Id = 4, Nome = "Alinhamento" }));
            var handler = new HandlerFalso().Responder(HttpStatusCode.OK, ItemJson);

            await Criar(handler).AtualizarAsync(10, 3, new ItemOrdemServicoUpdateDto { ServicoOficinaId = 4, DescricaoServico = "Alinhamento - dianteiro", Quantidade = 1, ValorUnitario = 90 });

            Assert.Equal("/api/ordem-servico/10/itens/3", handler.Chamadas.Single().Url);
            Assert.Contains("\"descricao\":\"Alinhamento - dianteiro\"", handler.Chamadas.Single().Corpo);
        }

        [Fact]
        public async Task Sem_estoque_servico_nem_descricao_nao_chama_o_servico()
        {
            var handler = new HandlerFalso();

            var resposta = await Criar(handler).AdicionarAsync(10, new ItemOrdemServicoCreateDto { Quantidade = 1, ValorUnitario = 10 });

            Assert.True(resposta.hasErrors);
            Assert.Empty(handler.Chamadas);
        }

        [Fact]
        public async Task Estoque_inexistente_devolve_o_erro_do_cadastro()
        {
            _estoque.Setup(e => e.ObterPorIdAsync("9")).ReturnsAsync(new ResponseDto<EstoqueDto>().addError("Registro não encontrado."));
            var handler = new HandlerFalso();

            var resposta = await Criar(handler).AdicionarAsync(10, new ItemOrdemServicoCreateDto { EstoqueId = 9, Quantidade = 1, ValorUnitario = 10 });

            Assert.Contains("Registro não encontrado.", resposta.getErrors);
            Assert.Empty(handler.Chamadas);
        }

        [Fact]
        public async Task Listar_e_remover_usam_a_OS_na_rota()
        {
            var handler = new HandlerFalso()
                .Responder(HttpStatusCode.OK, $"[{ItemJson}]")
                .Responder(HttpStatusCode.NoContent);

            var itens = await Criar(handler).ListarAsync(10);
            var removido = await Criar(handler).RemoverAsync(10, 3);

            Assert.Equal("Filtro de Ar", itens.getResponse.Single().DescricaoServico);
            Assert.True(removido.getResponse);
            Assert.Equal("/api/ordem-servico/10/itens/3", handler.Chamadas[1].Url);
        }
    }
}
