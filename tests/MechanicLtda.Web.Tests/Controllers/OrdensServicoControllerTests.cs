using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Web.Controllers;
using MechanicLtda.Web.Integracoes;
using MechanicLtda.Web.ViewModels;
using Moq;
using Xunit;
using static MechanicLtda.Web.Tests.Controllers.Apoio;

namespace MechanicLtda.Web.Tests.Controllers
{
    public class OrdensServicoControllerTests
    {
        private readonly Mock<IOrdemServicoCliente> _ordens = new();
        private readonly Mock<IClienteAppService> _clientes = new();
        private readonly Mock<IVeiculoAppService> _veiculos = new();
        private readonly Mock<IItensOrdemServicoCliente> _itens = new();
        private readonly Mock<IOrcamentoCliente> _orcamentos = new();

        public OrdensServicoControllerTests()
        {
            _clientes.Setup(c => c.ObterTodosAsync()).ReturnsAsync(Ok<IEnumerable<ClienteDto>>([new ClienteDto { Id = 1, Nome = "Ana", Ativo = true }]));
            _veiculos.Setup(v => v.ObterTodosAsync()).ReturnsAsync(Ok<IEnumerable<VeiculoDto>>(
                [new VeiculoDto { Id = 5, Placa = "ABC1D23", Marca = "Fiat", Modelo = "Uno", Ativo = true, ClienteId = 1 }]));
        }

        private OrdensServicoController Criar() =>
            Preparar(new OrdensServicoController(_ordens.Object, _clientes.Object, _veiculos.Object, _itens.Object, _orcamentos.Object));

        private static OrdemServicoDto Ordem => new() { Id = 10, ClienteId = 1, VeiculoId = 5, DescricaoProblema = "Barulho", Status = StatusOrdemServico.Recebida };

        [Fact]
        public async Task Index_traz_ordens_e_tempo_medio()
        {
            _ordens.Setup(o => o.ObterTodosAsync()).ReturnsAsync(Ok<IEnumerable<OrdemServicoDto>>([Ordem]));
            _ordens.Setup(o => o.ObterTempoMedioExecucaoAsync()).ReturnsAsync(Ok(new TempoMedioExecucaoDto { QuantidadeOrdensFinalizadas = 2 }));

            var model = Modelo<OrdensServicoIndexViewModel>(await Criar().Index());
            Assert.Single(model.Ordens);
            Assert.Equal(2, model.TempoMedioExecucao.QuantidadeOrdensFinalizadas);
        }

        [Fact]
        public async Task Index_com_erros_mostra_listas_vazias()
        {
            _ordens.Setup(o => o.ObterTodosAsync()).ReturnsAsync(Erro<IEnumerable<OrdemServicoDto>>("os fora"));
            _ordens.Setup(o => o.ObterTempoMedioExecucaoAsync()).ReturnsAsync(Erro<TempoMedioExecucaoDto>());

            var model = Modelo<OrdensServicoIndexViewModel>(await Criar().Index());
            Assert.Empty(model.Ordens);
            Assert.Equal(0, model.TempoMedioExecucao.QuantidadeOrdensFinalizadas);
        }

        [Fact]
        public async Task Details_monta_a_tela_com_itens_historico_e_orcamento()
        {
            _ordens.Setup(o => o.ObterPorIdAsync("10")).ReturnsAsync(Ok(Ordem));
            _itens.Setup(i => i.ListarAsync(10)).ReturnsAsync(Ok<IEnumerable<ItemOrdemServicoDto>>([new ItemOrdemServicoDto { Id = 1 }]));
            _ordens.Setup(o => o.ObterHistoricoAsync(10)).ReturnsAsync(Ok<IEnumerable<HistoricoStatusOrdemServicoDto>>(
                [new HistoricoStatusOrdemServicoDto(null, null, 1, "Recebida", DateTime.UtcNow, "Web", null)]));
            _orcamentos.Setup(o => o.ObterPorOrdemServicoAsync(10)).ReturnsAsync(Ok<OrcamentoDto?>(new OrcamentoDto { Id = 7 }));

            var model = Modelo<OrdemServicoDetalheViewModel>(await Criar().Details(10));
            Assert.Single(model.Itens);
            Assert.Single(model.Historico);
            Assert.Equal(7, model.Orcamento!.Id);
            Assert.Null(model.OrcamentoErro);
        }

        [Fact]
        public async Task Details_com_falhas_parciais_nao_derruba_a_tela()
        {
            _ordens.Setup(o => o.ObterPorIdAsync("10")).ReturnsAsync(Ok(Ordem));
            _itens.Setup(i => i.ListarAsync(10)).ReturnsAsync(Erro<IEnumerable<ItemOrdemServicoDto>>());
            _ordens.Setup(o => o.ObterHistoricoAsync(10)).ReturnsAsync(Erro<IEnumerable<HistoricoStatusOrdemServicoDto>>());
            _orcamentos.Setup(o => o.ObterPorOrdemServicoAsync(10)).ReturnsAsync(Erro<OrcamentoDto?>("billing fora"));

            var model = Modelo<OrdemServicoDetalheViewModel>(await Criar().Details(10));
            Assert.Empty(model.Itens);
            Assert.Empty(model.Historico);
            Assert.Null(model.Orcamento);
            Assert.Equal("billing fora", model.OrcamentoErro);
        }

        [Fact]
        public async Task Details_modelo_invalido_ou_os_inexistente_volta_para_index()
        {
            Redireciona(await Criar().InvalidarModelo().Details(10), "Index");

            _ordens.Setup(o => o.ObterPorIdAsync("10")).ReturnsAsync(Erro<OrdemServicoDto>());
            Redireciona(await Criar().Details(10), "Index");
        }

        [Fact]
        public async Task Create_get_preenche_clientes_e_veiculos()
        {
            var model = Modelo<OrdemServicoFormViewModel>(await Criar().Create());
            Assert.Single(model.Clientes);
            Assert.Equal("ABC1D23 - Fiat Uno", model.Veiculos.Single().Text);
        }

        [Fact]
        public async Task Selecoes_ficam_vazias_quando_os_cadastros_falham()
        {
            _clientes.Setup(c => c.ObterTodosAsync()).ReturnsAsync(Erro<IEnumerable<ClienteDto>>());
            _veiculos.Setup(v => v.ObterTodosAsync()).ReturnsAsync(Erro<IEnumerable<VeiculoDto>>());

            var model = Modelo<OrdemServicoFormViewModel>(await Criar().Create());
            Assert.Empty(model.Clientes);
            Assert.Empty(model.Veiculos);
        }

        [Fact]
        public async Task Create_post_cobre_modelo_invalido_erro_e_sucesso()
        {
            var form = new OrdemServicoFormViewModel { DescricaoProblema = "Barulho", ClienteId = 1, VeiculoId = 5 };
            Assert.NotEmpty(Modelo<OrdemServicoFormViewModel>(await Criar().InvalidarModelo().Create(form)).Clientes);

            _ordens.Setup(o => o.AdicionarAsync(It.IsAny<OrdemServicoCreateDto>())).ReturnsAsync(Erro<OrdemServicoDto>());
            Assert.Same(form, Modelo<OrdemServicoFormViewModel>(await Criar().Create(form)));

            _ordens.Setup(o => o.AdicionarAsync(It.Is<OrdemServicoCreateDto>(d => d.VeiculoId == 5))).ReturnsAsync(Ok(Ordem));
            var redirect = Redireciona(await Criar().Create(form), "Details");
            Assert.Equal(10, redirect.RouteValues!["id"]);
        }

        [Fact]
        public async Task Edit_get_cobre_modelo_invalido_erro_e_sucesso()
        {
            Redireciona(await Criar().InvalidarModelo().Edit(10), "Index");

            _ordens.Setup(o => o.ObterPorIdAsync("10")).ReturnsAsync(Erro<OrdemServicoDto>());
            Redireciona(await Criar().Edit(10), "Index");

            _ordens.Setup(o => o.ObterPorIdAsync("10")).ReturnsAsync(Ok(Ordem));
            var model = Modelo<OrdemServicoFormViewModel>(await Criar().Edit(10));
            Assert.Equal("Barulho", model.DescricaoProblema);
            Assert.True(model.Clientes.Single().Selected);
        }

        [Fact]
        public async Task Edit_post_cobre_modelo_invalido_erro_e_sucesso()
        {
            var form = new OrdemServicoFormViewModel { Id = 10, DescricaoProblema = "Barulho", ClienteId = 1, VeiculoId = 5 };
            Assert.Same(form, Modelo<OrdemServicoFormViewModel>(await Criar().InvalidarModelo().Edit(10, form)));

            _ordens.Setup(o => o.AtualizarAsync("10", It.IsAny<OrdemServicoUpdateDto>())).ReturnsAsync(Erro<OrdemServicoDto>());
            Assert.Same(form, Modelo<OrdemServicoFormViewModel>(await Criar().Edit(10, form)));

            _ordens.Setup(o => o.AtualizarAsync("10", It.IsAny<OrdemServicoUpdateDto>())).ReturnsAsync(Ok(Ordem));
            Redireciona(await Criar().Edit(10, form), "Details");
        }

        [Theory]
        [InlineData("diagnostico")]
        [InlineData("aprovacao")]
        [InlineData("aprovar")]
        [InlineData("recusar")]
        [InlineData("execucao")]
        [InlineData("finalizar")]
        [InlineData("entregar")]
        [InlineData("cancelar")]
        public async Task AlterarStatus_chama_a_transicao_certa(string acao)
        {
            var ok = Ok(Ordem);
            _ordens.Setup(o => o.IniciarDiagnosticoAsync("10")).ReturnsAsync(ok);
            _ordens.Setup(o => o.AguardarAprovacaoAsync("10")).ReturnsAsync(ok);
            _ordens.Setup(o => o.AprovarAsync("10")).ReturnsAsync(ok);
            _ordens.Setup(o => o.RecusarAsync("10", "caro")).ReturnsAsync(ok);
            _ordens.Setup(o => o.IniciarExecucaoAsync("10")).ReturnsAsync(ok);
            _ordens.Setup(o => o.FinalizarAsync("10")).ReturnsAsync(ok);
            _ordens.Setup(o => o.EntregarAsync("10")).ReturnsAsync(ok);
            _ordens.Setup(o => o.CancelarAsync(10, "desistiu")).ReturnsAsync(ok);

            var controller = Criar();
            Redireciona(await controller.AlterarStatus(10, acao, "caro", "desistiu"), "Details");
            Assert.NotNull(controller.TempData["Success"]);
        }

        [Fact]
        public async Task AlterarStatus_com_acao_desconhecida_ou_modelo_invalido_avisa_erro()
        {
            var controller = Criar();
            Redireciona(await controller.AlterarStatus(10, "voar"), "Details");
            Assert.Equal("Ação de status inválida.", controller.TempData["Error"]);

            Redireciona(await Criar().InvalidarModelo().AlterarStatus(10, "aprovar"), "Index");
        }

        [Fact]
        public async Task Recusar_sem_motivo_manda_texto_vazio()
        {
            _ordens.Setup(o => o.RecusarAsync("10", string.Empty)).ReturnsAsync(Ok(Ordem));
            Redireciona(await Criar().AlterarStatus(10, "recusar"), "Details");
            _ordens.Verify(o => o.RecusarAsync("10", string.Empty), Times.Once);
        }

        [Fact]
        public async Task Delete_cobre_modelo_invalido_erro_e_sucesso()
        {
            Redireciona(await Criar().InvalidarModelo().Delete(10), "Index");

            _ordens.Setup(o => o.RemoverAsync("10")).ReturnsAsync(Erro<bool>());
            Redireciona(await Criar().Delete(10), "Index");

            _ordens.Setup(o => o.RemoverAsync("10")).ReturnsAsync(Ok(true));
            Redireciona(await Criar().Delete(10), "Index");
        }
    }

    public class ItensOrdemServicoControllerTests
    {
        private readonly Mock<IItensOrdemServicoCliente> _itens = new();
        private readonly Mock<IEstoqueCliente> _estoque = new();
        private readonly Mock<IServicoOficinaCliente> _servicos = new();

        public ItensOrdemServicoControllerTests()
        {
            _estoque.Setup(e => e.ObterTodosAsync()).ReturnsAsync(Ok<IEnumerable<EstoqueDto>>([new EstoqueDto { Id = 2, Nome = "Filtro", Tipo = TipoEstoque.Peca }]));
            _servicos.Setup(s => s.ObterAtivosAsync()).ReturnsAsync(Ok<IEnumerable<ServicoOficinaDto>>([new ServicoOficinaDto { Id = 4, Nome = "Alinhamento", ValorBase = 100 }]));
        }

        private ItensOrdemServicoController Criar() => Preparar(new ItensOrdemServicoController(_itens.Object, _estoque.Object, _servicos.Object));

        private static ItemOrdemServicoDto Item => new() { Id = 1, OrdemServicoId = 10, EstoqueId = 2, Quantidade = 2, ValorUnitario = 40 };

        [Fact]
        public async Task Create_get_preenche_estoques_e_servicos()
        {
            var model = Modelo<ItemOrdemServicoFormViewModel>(await Criar().Create(10));
            Assert.Equal(10, model.OrdemServicoId);
            Assert.Equal("Filtro (Peca)", model.Estoques.Single().Text);
            Assert.Single(model.ServicosOficina);
        }

        [Fact]
        public async Task Create_get_com_modelo_invalido_volta_para_as_ordens()
        {
            var controller = Criar().InvalidarModelo();
            Redireciona(await controller.Create(10), "Index", "OrdensServico");
            Assert.Equal("O valor 'x' não é válido.", controller.TempData["Error"]);
        }

        [Fact]
        public async Task Selecoes_ficam_vazias_quando_a_execucao_falha()
        {
            _estoque.Setup(e => e.ObterTodosAsync()).ReturnsAsync(Erro<IEnumerable<EstoqueDto>>());
            _servicos.Setup(s => s.ObterAtivosAsync()).ReturnsAsync(Erro<IEnumerable<ServicoOficinaDto>>());

            var model = Modelo<ItemOrdemServicoFormViewModel>(await Criar().Create(10));
            Assert.Empty(model.Estoques);
            Assert.Empty(model.ServicosOficina);
        }

        [Fact]
        public async Task Create_post_cobre_modelo_invalido_erro_e_sucesso()
        {
            var form = new ItemOrdemServicoFormViewModel { OrdemServicoId = 10, EstoqueId = 2, Quantidade = 2, ValorUnitario = 40 };
            Assert.Same(form, Modelo<ItemOrdemServicoFormViewModel>(await Criar().InvalidarModelo().Create(form)));

            _itens.Setup(i => i.AdicionarAsync(10, It.IsAny<ItemOrdemServicoCreateDto>())).ReturnsAsync(Erro<ItemOrdemServicoDto>());
            Assert.Same(form, Modelo<ItemOrdemServicoFormViewModel>(await Criar().Create(form)));

            _itens.Setup(i => i.AdicionarAsync(10, It.Is<ItemOrdemServicoCreateDto>(d => d.Quantidade == 2))).ReturnsAsync(Ok(Item));
            Redireciona(await Criar().Create(form), "Details", "OrdensServico");
        }

        [Fact]
        public async Task Edit_get_cobre_modelo_invalido_erro_e_sucesso()
        {
            Redireciona(await Criar().InvalidarModelo().Edit(1, 10), "Index", "OrdensServico");

            _itens.Setup(i => i.ObterAsync(10, 1)).ReturnsAsync(Erro<ItemOrdemServicoDto>());
            Redireciona(await Criar().Edit(1, 10), "Index", "OrdensServico");

            _itens.Setup(i => i.ObterAsync(10, 1)).ReturnsAsync(Ok(Item));
            var model = Modelo<ItemOrdemServicoFormViewModel>(await Criar().Edit(1, 10));
            Assert.Equal(2, model.Quantidade);
            Assert.True(model.Estoques.Single().Selected);
        }

        [Fact]
        public async Task Edit_post_cobre_modelo_invalido_erro_e_sucesso()
        {
            var form = new ItemOrdemServicoFormViewModel { Id = 1, OrdemServicoId = 10, ServicoOficinaId = 4, Quantidade = 1, ValorUnitario = 100 };
            Assert.Same(form, Modelo<ItemOrdemServicoFormViewModel>(await Criar().InvalidarModelo().Edit(1, form)));

            _itens.Setup(i => i.AtualizarAsync(10, 1, It.IsAny<ItemOrdemServicoUpdateDto>())).ReturnsAsync(Erro<ItemOrdemServicoDto>());
            Assert.Same(form, Modelo<ItemOrdemServicoFormViewModel>(await Criar().Edit(1, form)));

            _itens.Setup(i => i.AtualizarAsync(10, 1, It.IsAny<ItemOrdemServicoUpdateDto>())).ReturnsAsync(Ok(Item));
            Redireciona(await Criar().Edit(1, form), "Details", "OrdensServico");
        }

        [Fact]
        public async Task Delete_cobre_modelo_invalido_erro_e_sucesso()
        {
            Redireciona(await Criar().InvalidarModelo().Delete(1, 10), "Index", "OrdensServico");

            _itens.Setup(i => i.RemoverAsync(10, 1)).ReturnsAsync(Erro<bool>());
            Redireciona(await Criar().Delete(1, 10), "Details", "OrdensServico");

            _itens.Setup(i => i.RemoverAsync(10, 1)).ReturnsAsync(Ok(true));
            Redireciona(await Criar().Delete(1, 10), "Details", "OrdensServico");
        }
    }
}
