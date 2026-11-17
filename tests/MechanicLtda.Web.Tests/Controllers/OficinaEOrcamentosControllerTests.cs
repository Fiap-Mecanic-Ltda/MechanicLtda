using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Web.Controllers;
using MechanicLtda.Web.Integracoes;
using MechanicLtda.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using static MechanicLtda.Web.Tests.Controllers.Apoio;

namespace MechanicLtda.Web.Tests.Controllers
{
    public class OficinaControllerTests
    {
        private readonly Mock<IOrdemServicoCliente> _ordens = new();
        private readonly Mock<IEstoqueCliente> _estoque = new();
        private readonly Mock<IServicoOficinaCliente> _servicos = new();
        private readonly Mock<IExecucaoCliente> _execucao = new();

        public OficinaControllerTests()
        {
            _estoque.Setup(e => e.ObterTodosAsync()).ReturnsAsync(Ok<IEnumerable<EstoqueDto>>(
            [
                new EstoqueDto { Id = 2, Nome = "Filtro", Tipo = TipoEstoque.Peca },
                new EstoqueDto { Id = 3, Nome = "Óleo", Tipo = TipoEstoque.Insumo }
            ]));
            _servicos.Setup(s => s.ObterAtivosAsync()).ReturnsAsync(Ok<IEnumerable<ServicoOficinaDto>>([new ServicoOficinaDto { Id = 4, Nome = "Alinhamento" }]));
        }

        private OficinaController Criar(bool saga = true) => Preparar(new OficinaController(
            _ordens.Object, _estoque.Object, _servicos.Object, _execucao.Object,
            Options.Create(new IntegracoesOptions { SagaHabilitada = saga })));

        private static ExecucaoDto Execucao(string status = "EmDiagnostico") => new(
            10, 1, 5, "Barulho", status, null, null, DateTime.UtcNow, DateTime.UtcNow, null, null, null, 0m, [], []);

        private void RespostaDiagnostico(ResponseDto<ExecucaoDto> resposta) =>
            _execucao.Setup(e => e.RegistrarDiagnosticoAsync(10, It.IsAny<IReadOnlyList<ItemDiagnosticoDto>>(), It.IsAny<string?>())).ReturnsAsync(resposta);

        [Fact]
        public async Task Index_mostra_a_fila_e_o_estado_da_saga()
        {
            _execucao.Setup(e => e.FilaAsync("NaFila")).ReturnsAsync(Ok<IEnumerable<FilaExecucaoDto>>(
                [new FilaExecucaoDto(10, "NaFila", "Barulho", DateTime.UtcNow, DateTime.UtcNow)]));

            var model = Modelo<OficinaIndexViewModel>(await Criar(saga: false).Index("NaFila"));
            Assert.Single(model.Fila);
            Assert.Equal("NaFila", model.Status);
            Assert.False(model.SagaHabilitada);
        }

        [Fact]
        public async Task Index_com_erro_mostra_fila_vazia()
        {
            _execucao.Setup(e => e.FilaAsync(null)).ReturnsAsync(Erro<IEnumerable<FilaExecucaoDto>>());
            Assert.Empty(Modelo<OficinaIndexViewModel>(await Criar().Index()).Fila);
        }

        [Fact]
        public async Task Details_em_diagnostico_carrega_estoque_e_servicos_em_ordem()
        {
            _execucao.Setup(e => e.ObterAsync(10)).ReturnsAsync(Ok(Execucao()));

            var model = Modelo<OficinaDetalheViewModel>(await Criar().Details(10));
            Assert.Equal(OficinaController.LinhasDiagnostico, model.Diagnostico.Itens.Count);
            Assert.Equal(["Filtro", "Óleo"], model.Estoques.Select(e => e.Nome));
            Assert.Single(model.Servicos);
            Assert.False(model.Final);
        }

        [Fact]
        public async Task Details_em_diagnostico_com_falha_nos_cadastros_deixa_listas_vazias()
        {
            _execucao.Setup(e => e.ObterAsync(10)).ReturnsAsync(Ok(Execucao()));
            _estoque.Setup(e => e.ObterTodosAsync()).ReturnsAsync(Erro<IEnumerable<EstoqueDto>>());
            _servicos.Setup(s => s.ObterAtivosAsync()).ReturnsAsync(Erro<IEnumerable<ServicoOficinaDto>>());

            var model = Modelo<OficinaDetalheViewModel>(await Criar().Details(10));
            Assert.Empty(model.Estoques);
            Assert.Empty(model.Servicos);
        }

        [Fact]
        public async Task Details_fora_do_diagnostico_nao_consulta_cadastros()
        {
            _execucao.Setup(e => e.ObterAsync(10)).ReturnsAsync(Ok(Execucao("Finalizada")));

            var model = Modelo<OficinaDetalheViewModel>(await Criar().Details(10));
            Assert.True(model.Final);
            _estoque.Verify(e => e.ObterTodosAsync(), Times.Never);
        }

        [Fact]
        public async Task Details_com_erro_ou_modelo_invalido_volta_para_a_fila()
        {
            Redireciona(await Criar().InvalidarModelo().Details(10), "Index");

            _execucao.Setup(e => e.ObterAsync(10)).ReturnsAsync(Erro<ExecucaoDto>());
            Redireciona(await Criar().Details(10), "Index");
        }

        [Fact]
        public async Task Receber_poe_a_os_na_fila()
        {
            _ordens.Setup(o => o.ObterPorIdAsync("10")).ReturnsAsync(Ok(new OrdemServicoDto { Id = 10, ClienteId = 1, VeiculoId = 5, DescricaoProblema = "Barulho" }));
            _execucao.Setup(e => e.ReceberAsync(10, 1, 5, "Barulho")).ReturnsAsync(Ok(Execucao("AguardandoDiagnostico")));

            var controller = Criar();
            Redireciona(await controller.Receber(10), "Details");
            Assert.NotNull(controller.TempData["Success"]);
        }

        [Fact]
        public async Task Receber_com_os_inexistente_ou_modelo_invalido_volta_para_a_fila()
        {
            Redireciona(await Criar().InvalidarModelo().Receber(10), "Index");

            _ordens.Setup(o => o.ObterPorIdAsync("10")).ReturnsAsync(Erro<OrdemServicoDto>());
            Redireciona(await Criar().Receber(10), "Index");
        }

        [Theory]
        [InlineData("assumir", null)]
        [InlineData("falha", "sem peça")]
        [InlineData("liberar", null)]
        [InlineData("iniciar", null)]
        [InlineData("finalizar", "ok")]
        [InlineData("cancelar", "desistiu")]
        public async Task Acao_chama_a_transicao_da_execucao(string acao, string? texto)
        {
            var ok = Ok(Execucao());
            _execucao.Setup(e => e.IniciarDiagnosticoAsync(10)).ReturnsAsync(ok);
            _execucao.Setup(e => e.FalhaDiagnosticoAsync(10, "sem peça")).ReturnsAsync(ok);
            _execucao.Setup(e => e.LiberarExecucaoAsync(10)).ReturnsAsync(ok);
            _execucao.Setup(e => e.IniciarAsync(10)).ReturnsAsync(ok);
            _execucao.Setup(e => e.FinalizarAsync(10, "ok")).ReturnsAsync(ok);
            _execucao.Setup(e => e.CancelarAsync(10, "desistiu")).ReturnsAsync(ok);

            var controller = Criar();
            Redireciona(await controller.Acao(10, acao, texto), "Details");
            Assert.Equal("Execução atualizada.", controller.TempData["Success"]);
        }

        [Theory]
        [InlineData("falha", " ", "Informe o motivo da falha no diagnóstico.")]
        [InlineData("voar", null, "Ação inválida.")]
        public async Task Acao_invalida_avisa_sem_chamar_a_execucao(string acao, string? texto, string erro)
        {
            var controller = Criar();
            Redireciona(await controller.Acao(10, acao, texto), "Details");
            Assert.Equal(erro, controller.TempData["Error"]);
        }

        [Fact]
        public async Task Acao_com_modelo_invalido_volta_para_a_fila()
        {
            Redireciona(await Criar().InvalidarModelo().Acao(10, "assumir"), "Index");
        }

        [Fact]
        public async Task Diagnostico_converte_as_linhas_e_ignora_as_vazias()
        {
            IReadOnlyList<ItemDiagnosticoDto>? enviados = null;
            _execucao.Setup(e => e.RegistrarDiagnosticoAsync(10, It.IsAny<IReadOnlyList<ItemDiagnosticoDto>>(), "obs"))
                .Callback<int, IReadOnlyList<ItemDiagnosticoDto>, string?>((_, itens, _) => enviados = itens)
                .ReturnsAsync(Ok(Execucao()));

            var form = new DiagnosticoFormViewModel
            {
                Observacao = "obs",
                Itens =
                [
                    new LinhaDiagnosticoViewModel { Origem = "E:2", Quantidade = 2 },
                    new LinhaDiagnosticoViewModel { Origem = "E:3", Quantidade = 1, Descricao = "  5W30 " },
                    new LinhaDiagnosticoViewModel { Origem = "S:4", Quantidade = 1, ValorUnitario = 90 },
                    new LinhaDiagnosticoViewModel { Origem = null, Quantidade = null }
                ]
            };

            var controller = Criar();
            Redireciona(await controller.Diagnostico(10, form), "Details");
            Assert.NotNull(controller.TempData["Success"]);
            Assert.Equal(["Peca", "Insumo", "Servico"], enviados!.Select(i => i.Tipo));
            Assert.Equal("5W30", enviados[1].Descricao);
            Assert.Equal(4, enviados[2].ServicoOficinaId);
        }

        [Fact]
        public async Task Diagnostico_aponta_linhas_sem_quantidade_ou_item_desconhecido()
        {
            var form = new DiagnosticoFormViewModel
            {
                Itens =
                [
                    new LinhaDiagnosticoViewModel { Origem = "E:2", Quantidade = null },
                    new LinhaDiagnosticoViewModel { Origem = "E:99", Quantidade = 1 },
                    new LinhaDiagnosticoViewModel { Origem = "X:1", Quantidade = 1 }
                ]
            };

            var controller = Criar();
            Redireciona(await controller.Diagnostico(10, form), "Details");
            var erro = Assert.IsType<string>(controller.TempData["Error"]);
            Assert.Contains("Linha 1: informe a quantidade.", erro);
            Assert.Contains("Linha 2: item de estoque ou serviço não encontrado.", erro);
            _execucao.Verify(e => e.RegistrarDiagnosticoAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<ItemDiagnosticoDto>>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task Diagnostico_sem_itens_pede_ao_menos_um()
        {
            _estoque.Setup(e => e.ObterTodosAsync()).ReturnsAsync(Erro<IEnumerable<EstoqueDto>>());
            var controller = Criar();
            Redireciona(await controller.Diagnostico(10, new DiagnosticoFormViewModel { Itens = [new LinhaDiagnosticoViewModel()] }), "Details");
            Assert.Equal("Registre ao menos um item no diagnóstico.", controller.TempData["Error"]);
        }

        [Fact]
        public async Task Diagnostico_recusado_pela_execucao_mostra_o_erro()
        {
            RespostaDiagnostico(Erro<ExecucaoDto>("sem estoque"));
            var controller = Criar();
            var form = new DiagnosticoFormViewModel { Itens = [new LinhaDiagnosticoViewModel { Origem = "S:4", Quantidade = 1 }] };

            Redireciona(await controller.Diagnostico(10, form), "Details");
            Assert.Equal("sem estoque", controller.TempData["Error"]);
        }

        [Fact]
        public async Task Diagnostico_com_modelo_invalido_volta_para_a_fila()
        {
            Redireciona(await Criar().InvalidarModelo().Diagnostico(10, new DiagnosticoFormViewModel()), "Index");
        }
    }

    public class OrcamentosControllerTests
    {
        private readonly Mock<IOrcamentoCliente> _orcamentos = new();
        private readonly Mock<IPagamentoCliente> _pagamentos = new();
        private readonly Mock<IOrdemServicoCliente> _ordens = new();
        private readonly Mock<IClienteAppService> _clientes = new();

        private OrcamentosController Criar(string publicKey = "") => Preparar(new OrcamentosController(
            _orcamentos.Object, _pagamentos.Object, _ordens.Object, _clientes.Object,
            Options.Create(new IntegracoesOptions { OrcamentoService = new OrcamentoServicoOptions { MercadoPagoPublicKey = publicKey } })));

        private static OrcamentoDto Orcamento(StatusOrcamento status = StatusOrcamento.Aberto) =>
            new() { Id = 7, OrdemServicoId = 10, Status = status, ValorTotalGeral = 450 };

        private static PagamentoFormViewModel Form => new()
        {
            OrcamentoId = 7, OrdemServicoId = 10, RequisicaoId = Guid.Empty, Token = "APRO", PaymentMethodId = "master",
            Email = " ana@x.com ", TipoDocumento = null, NumeroDocumento = "529.982.247-25"
        };

        private void Pagamento(StatusPagamento status, string? detalhe = null) =>
            _pagamentos.Setup(p => p.CriarAsync(It.IsAny<PagamentoCreateDto>()))
                .ReturnsAsync(Ok(new PagamentoDto { Id = 1, Status = status, StatusDetalhe = detalhe }));

        [Fact]
        public async Task Details_traz_orcamento_e_pagamentos_mais_recentes_primeiro()
        {
            _ordens.Setup(o => o.ObterPorIdAsync("10")).ReturnsAsync(Ok(new OrdemServicoDto { Id = 10 }));
            _orcamentos.Setup(o => o.ObterPorOrdemServicoAsync(10)).ReturnsAsync(Ok<OrcamentoDto?>(Orcamento()));
            _pagamentos.Setup(p => p.ListarPorOrcamentoAsync(7)).ReturnsAsync(Ok<IEnumerable<PagamentoDto>>(
            [
                new PagamentoDto { Id = 1, CriadoEm = DateTime.UtcNow.AddHours(-1) },
                new PagamentoDto { Id = 2, CriadoEm = DateTime.UtcNow, Status = StatusPagamento.Aprovado }
            ]));

            var model = Modelo<OrcamentoDetalheViewModel>(await Criar().Details(10));
            Assert.Equal(7, model.Orcamento!.Id);
            Assert.Equal([2, 1], model.Pagamentos.Select(p => p.Id));
            Assert.True(model.Pago);
        }

        [Fact]
        public async Task Details_sem_orcamento_ou_com_erro_nos_pagamentos_mostra_vazio()
        {
            _ordens.Setup(o => o.ObterPorIdAsync("10")).ReturnsAsync(Ok(new OrdemServicoDto { Id = 10 }));
            _orcamentos.Setup(o => o.ObterPorOrdemServicoAsync(10)).ReturnsAsync(Ok<OrcamentoDto?>(Orcamento()));
            _pagamentos.Setup(p => p.ListarPorOrcamentoAsync(7)).ReturnsAsync(Erro<IEnumerable<PagamentoDto>>());
            Assert.Empty(Modelo<OrcamentoDetalheViewModel>(await Criar().Details(10)).Pagamentos);

            _orcamentos.Setup(o => o.ObterPorOrdemServicoAsync(10)).ReturnsAsync(Erro<OrcamentoDto?>());
            Assert.Null(Modelo<OrcamentoDetalheViewModel>(await Criar().Details(10)).Orcamento);
        }

        [Fact]
        public async Task Details_com_os_inexistente_ou_modelo_invalido_volta_para_as_ordens()
        {
            Redireciona(await Criar().InvalidarModelo().Details(10), "Index", "OrdensServico");

            _ordens.Setup(o => o.ObterPorIdAsync("10")).ReturnsAsync(Erro<OrdemServicoDto>());
            Redireciona(await Criar().Details(10), "Index", "OrdensServico");
        }

        [Fact]
        public async Task Pdf_devolve_o_arquivo_ou_volta_com_erro()
        {
            _orcamentos.Setup(o => o.ObterPdfAsync(7)).ReturnsAsync(Ok(new byte[] { 1, 2 }));
            var arquivo = Assert.IsType<FileContentResult>(await Criar().Pdf(7, 10));
            Assert.Equal("orcamento-000007.pdf", arquivo.FileDownloadName);

            _orcamentos.Setup(o => o.ObterPdfAsync(7)).ReturnsAsync(Erro<byte[]>());
            Redireciona(await Criar().Pdf(7, 10), "Details");

            Redireciona(await Criar().InvalidarModelo().Pdf(7, 10), "Index", "OrdensServico");
        }

        [Fact]
        public async Task Aprovar_e_recusar_avisam_o_resultado()
        {
            _orcamentos.Setup(o => o.AprovarAsync(7)).ReturnsAsync(Ok(Orcamento(StatusOrcamento.Aprovado)));
            _orcamentos.Setup(o => o.RecusarAsync(7, "caro")).ReturnsAsync(Ok(Orcamento(StatusOrcamento.Recusado)));
            var aprovado = Criar();
            Redireciona(await aprovado.Aprovar(7, 10), "Details");
            Assert.NotNull(aprovado.TempData["Success"]);
            var recusado = Criar();
            Redireciona(await recusado.Recusar(7, 10, "caro"), "Details");
            Assert.NotNull(recusado.TempData["Success"]);

            _orcamentos.Setup(o => o.AprovarAsync(7)).ReturnsAsync(Erro<OrcamentoDto>("pago"));
            _orcamentos.Setup(o => o.RecusarAsync(7, null)).ReturnsAsync(Erro<OrcamentoDto>("pago"));
            var erro = Criar();
            Redireciona(await erro.Aprovar(7, 10), "Details");
            Redireciona(await erro.Recusar(7, 10, null), "Details");
            Assert.Equal("pago", erro.TempData["Error"]);

            Redireciona(await Criar().InvalidarModelo().Aprovar(7, 10), "Index", "OrdensServico");
            Redireciona(await Criar().InvalidarModelo().Recusar(7, 10, null), "Index", "OrdensServico");
        }

        [Fact]
        public async Task Pagar_get_sugere_os_dados_do_cliente()
        {
            _orcamentos.Setup(o => o.ObterPorIdAsync(7)).ReturnsAsync(Ok(Orcamento()));
            _ordens.Setup(o => o.ObterPorIdAsync("10")).ReturnsAsync(Ok(new OrdemServicoDto { Id = 10, ClienteId = 1 }));
            _clientes.Setup(c => c.ObterPorIdAsync("1")).ReturnsAsync(Ok(new ClienteDto { Id = 1, Email = "ana@x.com", CpfCnpj = "529.982.247-25" }));

            var model = Modelo<PagamentoFormViewModel>(await Criar("TEST-key").Pagar(7));
            Assert.Equal("ana@x.com", model.Email);
            Assert.Equal("52998224725", model.NumeroDocumento);
            Assert.Equal("CPF", model.TipoDocumento);
            Assert.NotEqual(Guid.Empty, model.RequisicaoId);
            Assert.False(model.ModoSimulado);
        }

        [Fact]
        public async Task Pagar_get_com_cnpj_e_sem_cliente()
        {
            _orcamentos.Setup(o => o.ObterPorIdAsync(7)).ReturnsAsync(Ok(Orcamento()));
            _ordens.Setup(o => o.ObterPorIdAsync("10")).ReturnsAsync(Ok(new OrdemServicoDto { Id = 10, ClienteId = 1 }));
            _clientes.Setup(c => c.ObterPorIdAsync("1")).ReturnsAsync(Ok(new ClienteDto { Id = 1, Email = "e@x.com", CpfCnpj = "11.222.333/0001-81" }));
            Assert.Equal("CNPJ", Modelo<PagamentoFormViewModel>(await Criar().Pagar(7)).TipoDocumento);

            _clientes.Setup(c => c.ObterPorIdAsync("1")).ReturnsAsync(Erro<ClienteDto>());
            Assert.Equal(string.Empty, Modelo<PagamentoFormViewModel>(await Criar().Pagar(7)).Email);

            _ordens.Setup(o => o.ObterPorIdAsync("10")).ReturnsAsync(Erro<OrdemServicoDto>());
            Assert.True(Modelo<PagamentoFormViewModel>(await Criar().Pagar(7)).ModoSimulado);
        }

        [Fact]
        public async Task Pagar_get_bloqueia_orcamento_fechado_inexistente_ou_invalido()
        {
            _orcamentos.Setup(o => o.ObterPorIdAsync(7)).ReturnsAsync(Ok(Orcamento(StatusOrcamento.Pago)));
            var fechado = Criar();
            Redireciona(await fechado.Pagar(7), "Details");
            Assert.Equal("O orçamento está pago e não aceita pagamento.", fechado.TempData["Error"]);

            _orcamentos.Setup(o => o.ObterPorIdAsync(7)).ReturnsAsync(Erro<OrcamentoDto>());
            Redireciona(await Criar().Pagar(7), "Index", "OrdensServico");

            Redireciona(await Criar().InvalidarModelo().Pagar(7), "Index", "OrdensServico");
        }

        [Fact]
        public async Task Pagar_post_aprovado_monta_o_pedido_e_avisa()
        {
            PagamentoCreateDto? enviado = null;
            _pagamentos.Setup(p => p.CriarAsync(It.IsAny<PagamentoCreateDto>()))
                .Callback<PagamentoCreateDto>(d => enviado = d)
                .ReturnsAsync(Ok(new PagamentoDto { Status = StatusPagamento.Aprovado }));

            var controller = Criar();
            Redireciona(await controller.Pagar(Form), "Details");
            Assert.NotNull(controller.TempData["Success"]);
            Assert.Equal("ana@x.com", enviado!.Payer.Email);
            Assert.Equal(new IdentificacaoDto("CPF", "52998224725"), enviado.Payer.Identification);
            Assert.NotEqual(Guid.Empty, enviado.RequisicaoId);
        }

        [Theory]
        [InlineData(StatusPagamento.Pendente)]
        [InlineData(StatusPagamento.EmProcessamento)]
        [InlineData(StatusPagamento.Autorizado)]
        public async Task Pagar_post_em_aberto_pede_para_atualizar(StatusPagamento status)
        {
            Pagamento(status);
            var controller = Criar();
            Redireciona(await controller.Pagar(Form), "Details");
            Assert.Contains("Atualizar status", (string)controller.TempData["Success"]!);
        }

        [Theory]
        [InlineData(null, "Pagamento recusado. Tente outro cartão.")]
        [InlineData("cc_rejected", "Pagamento recusado (cc_rejected). Tente outro cartão.")]
        public async Task Pagar_post_recusado_sugere_outro_cartao(string? detalhe, string mensagem)
        {
            Pagamento(StatusPagamento.Recusado, detalhe);
            var controller = Criar();
            var form = Form;
            form.NumeroDocumento = null;
            Redireciona(await controller.Pagar(form), "Details");
            Assert.Equal(mensagem, controller.TempData["Error"]);
        }

        [Fact]
        public async Task Pagar_post_com_erro_reexibe_sem_o_token()
        {
            _pagamentos.Setup(p => p.CriarAsync(It.IsAny<PagamentoCreateDto>())).ReturnsAsync(Erro<PagamentoDto>("cartão inválido"));
            _orcamentos.Setup(o => o.ObterPorIdAsync(7)).ReturnsAsync(Ok(Orcamento()));

            var controller = Criar();
            var view = Assert.IsType<ViewResult>(await controller.Pagar(Form));
            Assert.Equal("Pagar", view.ViewName);
            var model = Assert.IsType<PagamentoFormViewModel>(view.Model);
            Assert.Equal(string.Empty, model.Token);
            Assert.Equal(7, model.Orcamento!.Id);
        }

        [Fact]
        public async Task Pagar_post_invalido_reexibe_ou_volta_se_o_orcamento_sumiu()
        {
            _orcamentos.Setup(o => o.ObterPorIdAsync(7)).ReturnsAsync(Ok(Orcamento()));
            Assert.IsType<ViewResult>(await Criar().InvalidarModelo().Pagar(Form));

            _orcamentos.Setup(o => o.ObterPorIdAsync(7)).ReturnsAsync(Erro<OrcamentoDto>());
            Redireciona(await Criar().InvalidarModelo().Pagar(Form), "Details");
        }

        [Fact]
        public async Task Sincronizar_cobre_erro_sem_mudanca_e_mudanca()
        {
            _pagamentos.Setup(p => p.SincronizarAsync(1)).ReturnsAsync(Erro<PagamentoDto?>("fora"));
            var erro = Criar();
            Redireciona(await erro.Sincronizar(1, 10), "Details");
            Assert.Equal("fora", erro.TempData["Error"]);

            _pagamentos.Setup(p => p.SincronizarAsync(1)).ReturnsAsync(Ok<PagamentoDto?>(null));
            var semMudanca = Criar();
            Redireciona(await semMudanca.Sincronizar(1, 10), "Details");
            Assert.Equal("O Mercado Pago não trouxe mudança no pagamento.", semMudanca.TempData["Success"]);

            _pagamentos.Setup(p => p.SincronizarAsync(1)).ReturnsAsync(Ok<PagamentoDto?>(new PagamentoDto { Status = StatusPagamento.Aprovado }));
            var mudou = Criar();
            Redireciona(await mudou.Sincronizar(1, 10), "Details");
            Assert.Equal("Pagamento atualizado: aprovado.", mudou.TempData["Success"]);

            Redireciona(await Criar().InvalidarModelo().Sincronizar(1, 10), "Index", "OrdensServico");
        }
    }
}
