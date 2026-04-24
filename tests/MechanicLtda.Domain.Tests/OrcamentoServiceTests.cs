using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Domain.Interfaces.Services;
using MechanicLtda.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MechanicLtda.Domain.Tests;

public class OrcamentoServiceTests
{
    private readonly Mock<IOrcamentoRepository>          _orcamentoRepositoryMock;
    private readonly Mock<IOrdemServicoRepository>       _ordemServicoRepositoryMock;
    private readonly Mock<IItemOrdemServicoRepository>   _itemRepositoryMock;
    private readonly Mock<IEstoqueRepository>            _estoqueRepositoryMock;
    private readonly Mock<INotificadorService>           _notificadorMock;
    private readonly Mock<ILogger<OrcamentoService>>     _loggerMock;
    private readonly Mock<IConfiguration>               _configurationMock;
    private readonly OrcamentoService                   _sut;

    public OrcamentoServiceTests()
    {
        _orcamentoRepositoryMock    = new Mock<IOrcamentoRepository>();
        _ordemServicoRepositoryMock = new Mock<IOrdemServicoRepository>();
        _itemRepositoryMock         = new Mock<IItemOrdemServicoRepository>();
        _estoqueRepositoryMock      = new Mock<IEstoqueRepository>();
        _notificadorMock            = new Mock<INotificadorService>();
        _loggerMock                 = new Mock<ILogger<OrcamentoService>>();
        _configurationMock          = new Mock<IConfiguration>();

        _sut = new OrcamentoService(
            _loggerMock.Object,
            _configurationMock.Object,
            _notificadorMock.Object,
            _orcamentoRepositoryMock.Object,
            _ordemServicoRepositoryMock.Object,
            _itemRepositoryMock.Object,
            _estoqueRepositoryMock.Object);
    }

    // ─── helpers ────────────────────────────────────────────────────────────────

    private static OrdemServico CriarOrdemServico(int id = 1) =>
        new()
        {
            Id                = id,
            DescricaoProblema = "Barulho no motor",
            VeiculoId         = 1,
            ClienteId         = 1,
            DataCriacao       = DateTime.Now
        };

    private static Orcamento CriarOrcamento(
        int id             = 1,
        int ordemServicoId = 1,
        decimal pecas      = 200m,
        decimal insumos    = 100m) =>
        new()
        {
            Id                = id,
            OrdemServicoId    = ordemServicoId,
            ValorTotalPecas   = pecas,
            ValorTotalInsumos = insumos,
            ValorTotalGeral   = pecas + insumos,
            DataGeracao       = DateTime.UtcNow,
            Validade          = DateTime.UtcNow.AddDays(30)
        };

    private static ItemOrdemServico CriarItem(
        int id             = 1,
        int ordemServicoId = 1,
        int? estoqueId     = null,
        int quantidade     = 2,
        decimal valor      = 100m) =>
        new()
        {
            Id             = id,
            OrdemServicoId = ordemServicoId,
            EstoqueId      = estoqueId,
            Quantidade     = quantidade,
            ValorUnitario  = valor,
            ValorTotal     = quantidade * valor
        };

    private static Estoque CriarEstoque(int id = 1, TipoEstoque tipo = TipoEstoque.Peca) =>
        new()
        {
            Id                    = id,
            Nome                  = "Item de Teste",
            Tipo                  = tipo,
            QuantidadeAtual       = 10,
            QuantidadeMinima      = 2,
            DataUltimaAtualizacao = DateTime.UtcNow
        };

    // ─── CriarOuAtualizarAsync ──────────────────────────────────────────────────

    #region CriarOuAtualizarAsync

    [Fact]
    public async Task CriarOuAtualizarAsync_QuandoOSNaoEncontrada_DeveLancarKeyNotFoundException()
    {
        // Arrange
        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(It.IsAny<string>()))
            .ReturnsAsync((OrdemServico?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.CriarOuAtualizarAsync(99));

        _orcamentoRepositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Orcamento>()), Times.Never);
        _orcamentoRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Orcamento>()), Times.Never);
    }

    [Fact]
    public async Task CriarOuAtualizarAsync_QuandoNaoExisteOrcamento_DeveCriarNovo()
    {
        // Arrange
        var os    = CriarOrdemServico();
        var itens = new List<ItemOrdemServico>();

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(os);

        _itemRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync(itens);

        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync((Orcamento?)null);

        Orcamento orcamentoCriado = null!;
        _orcamentoRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Orcamento>()))
            .Callback<Orcamento>(o => orcamentoCriado = o)
            .ReturnsAsync((Orcamento o) => o);

        // Act
        await _sut.CriarOuAtualizarAsync(1);

        // Assert
        Assert.NotNull(orcamentoCriado);
        Assert.Equal(1, orcamentoCriado.OrdemServicoId);
        _orcamentoRepositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Orcamento>()), Times.Once);
        _orcamentoRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Orcamento>()), Times.Never);
    }

    [Fact]
    public async Task CriarOuAtualizarAsync_QuandoJaExisteOrcamento_DeveAtualizarExistente()
    {
        // Arrange
        var os        = CriarOrdemServico();
        var existente = CriarOrcamento();
        var itens     = new List<ItemOrdemServico>();

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(os);

        _itemRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync(itens);

        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync(existente);

        _orcamentoRepositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<Orcamento>()))
            .ReturnsAsync(existente);

        // Act
        await _sut.CriarOuAtualizarAsync(1);

        // Assert
        _orcamentoRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Orcamento>()), Times.Once);
        _orcamentoRepositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Orcamento>()), Times.Never);
    }

    [Fact]
    public async Task CriarOuAtualizarAsync_SemItens_DeveGerarOrcamentoComValoresZerados()
    {
        // Arrange
        var os = CriarOrdemServico();

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(os);

        _itemRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync([]);

        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync((Orcamento?)null);

        Orcamento orcamentoCriado = null!;
        _orcamentoRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Orcamento>()))
            .Callback<Orcamento>(o => orcamentoCriado = o)
            .ReturnsAsync((Orcamento o) => o);

        // Act
        await _sut.CriarOuAtualizarAsync(1);

        // Assert
        Assert.Equal(0m, orcamentoCriado.ValorTotalPecas);
        Assert.Equal(0m, orcamentoCriado.ValorTotalInsumos);
        Assert.Equal(0m, orcamentoCriado.ValorTotalGeral);
    }

    [Fact]
    public async Task CriarOuAtualizarAsync_ComItensSemEstoque_DeveSomarTudoNoTotalGeral()
    {
        // Arrange
        var os    = CriarOrdemServico();
        var item1 = CriarItem(id: 1, estoqueId: null, quantidade: 2, valor: 100m); // 200
        var item2 = CriarItem(id: 2, estoqueId: null, quantidade: 1, valor: 50m);  // 50

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(os);

        _itemRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync([item1, item2]);

        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync((Orcamento?)null);

        Orcamento orcamentoCriado = null!;
        _orcamentoRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Orcamento>()))
            .Callback<Orcamento>(o => orcamentoCriado = o)
            .ReturnsAsync((Orcamento o) => o);

        // Act
        await _sut.CriarOuAtualizarAsync(1);

        // Assert
        Assert.Equal(250m, orcamentoCriado.ValorTotalGeral);  // 200 + 50
        Assert.Equal(0m, orcamentoCriado.ValorTotalPecas);    // sem vínculo de estoque
        Assert.Equal(0m, orcamentoCriado.ValorTotalInsumos);
    }

    [Fact]
    public async Task CriarOuAtualizarAsync_ComItensDePeca_DeveSomarEmValorTotalPecas()
    {
        // Arrange
        var os      = CriarOrdemServico();
        var estoque = CriarEstoque(id: 5, tipo: TipoEstoque.Peca);
        var item    = CriarItem(id: 1, estoqueId: 5, quantidade: 2, valor: 150m); // 300

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(os);

        _itemRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync([item]);

        _estoqueRepositoryMock
            .Setup(r => r.ObterPorIdAsync("5"))
            .ReturnsAsync(estoque);

        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync((Orcamento?)null);

        Orcamento orcamentoCriado = null!;
        _orcamentoRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Orcamento>()))
            .Callback<Orcamento>(o => orcamentoCriado = o)
            .ReturnsAsync((Orcamento o) => o);

        // Act
        await _sut.CriarOuAtualizarAsync(1);

        // Assert
        Assert.Equal(300m, orcamentoCriado.ValorTotalPecas);
        Assert.Equal(0m,   orcamentoCriado.ValorTotalInsumos);
        Assert.Equal(300m, orcamentoCriado.ValorTotalGeral);
    }

    [Fact]
    public async Task CriarOuAtualizarAsync_ComItensDeInsumo_DeveSomarEmValorTotalInsumos()
    {
        // Arrange
        var os      = CriarOrdemServico();
        var estoque = CriarEstoque(id: 3, tipo: TipoEstoque.Insumo);
        var item    = CriarItem(id: 1, estoqueId: 3, quantidade: 4, valor: 25m); // 100

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(os);

        _itemRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync([item]);

        _estoqueRepositoryMock
            .Setup(r => r.ObterPorIdAsync("3"))
            .ReturnsAsync(estoque);

        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync((Orcamento?)null);

        Orcamento orcamentoCriado = null!;
        _orcamentoRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Orcamento>()))
            .Callback<Orcamento>(o => orcamentoCriado = o)
            .ReturnsAsync((Orcamento o) => o);

        // Act
        await _sut.CriarOuAtualizarAsync(1);

        // Assert
        Assert.Equal(0m,   orcamentoCriado.ValorTotalPecas);
        Assert.Equal(100m, orcamentoCriado.ValorTotalInsumos);
        Assert.Equal(100m, orcamentoCriado.ValorTotalGeral);
    }

    [Fact]
    public async Task CriarOuAtualizarAsync_ComItensMistosEComSemEstoque_DeveSegmentarCorretamente()
    {
        // Arrange
        var os          = CriarOrdemServico();
        var estoquePeca = CriarEstoque(id: 1, tipo: TipoEstoque.Peca);
        var estoqueInsumo = CriarEstoque(id: 2, tipo: TipoEstoque.Insumo);

        var itemPeca    = CriarItem(id: 1, estoqueId: 1, quantidade: 2, valor: 100m); // 200 → Peça
        var itemInsumo  = CriarItem(id: 2, estoqueId: 2, quantidade: 3, valor: 20m);  //  60 → Insumo
        var itemSemVinculo = CriarItem(id: 3, estoqueId: null, quantidade: 1, valor: 50m); // 50 → geral

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(os);

        _itemRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync([itemPeca, itemInsumo, itemSemVinculo]);

        _estoqueRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(estoquePeca);

        _estoqueRepositoryMock
            .Setup(r => r.ObterPorIdAsync("2"))
            .ReturnsAsync(estoqueInsumo);

        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync((Orcamento?)null);

        Orcamento orcamentoCriado = null!;
        _orcamentoRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Orcamento>()))
            .Callback<Orcamento>(o => orcamentoCriado = o)
            .ReturnsAsync((Orcamento o) => o);

        // Act
        await _sut.CriarOuAtualizarAsync(1);

        // Assert
        Assert.Equal(200m, orcamentoCriado.ValorTotalPecas);
        Assert.Equal(60m,  orcamentoCriado.ValorTotalInsumos);
        Assert.Equal(310m, orcamentoCriado.ValorTotalGeral);  // 200 + 60 + 50
    }

    [Fact]
    public async Task CriarOuAtualizarAsync_DevePersistirDataGeracao()
    {
        // Arrange
        var os = CriarOrdemServico();

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(os);

        _itemRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync([]);

        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync((Orcamento?)null);

        Orcamento orcamentoCriado = null!;
        _orcamentoRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Orcamento>()))
            .Callback<Orcamento>(o => orcamentoCriado = o)
            .ReturnsAsync((Orcamento o) => o);

        // Act
        await _sut.CriarOuAtualizarAsync(1);

        // Assert
        Assert.NotEqual(default, orcamentoCriado.DataGeracao);
    }

    [Fact]
    public async Task CriarOuAtualizarAsync_DevePersistirValidadeComTrintaDias()
    {
        // Arrange
        var os = CriarOrdemServico();

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(os);

        _itemRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync([]);

        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync((Orcamento?)null);

        Orcamento orcamentoCriado = null!;
        _orcamentoRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Orcamento>()))
            .Callback<Orcamento>(o => orcamentoCriado = o)
            .ReturnsAsync((Orcamento o) => o);

        var antes = DateTime.UtcNow;

        // Act
        await _sut.CriarOuAtualizarAsync(1);

        // Assert — validade deve ser aprox. 30 dias a partir de agora
        Assert.NotNull(orcamentoCriado.Validade);
        Assert.True(orcamentoCriado.Validade!.Value >= antes.AddDays(29));
        Assert.True(orcamentoCriado.Validade!.Value <= DateTime.UtcNow.AddDays(31));
    }

    #endregion

    // ─── AtualizarManualAsync ───────────────────────────────────────────────────

    #region AtualizarManualAsync

    [Fact]
    public async Task AtualizarManualAsync_QuandoOrcamentoExiste_DeveAtualizarValores()
    {
        // Arrange
        var existente  = CriarOrcamento(pecas: 200m, insumos: 100m);
        var modificado = CriarOrcamento(pecas: 350m, insumos: 150m);

        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(existente);

        Orcamento salvo = null!;
        _orcamentoRepositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<Orcamento>()))
            .Callback<Orcamento>(o => salvo = o)
            .ReturnsAsync((Orcamento o) => o);

        // Act
        await _sut.AtualizarManualAsync(modificado);

        // Assert
        Assert.Equal(350m, salvo.ValorTotalPecas);
        Assert.Equal(150m, salvo.ValorTotalInsumos);
        _orcamentoRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Orcamento>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarManualAsync_DeveRecalcularValorTotalGeral()
    {
        // Arrange
        var existente  = CriarOrcamento(pecas: 100m, insumos: 50m);
        var modificado = CriarOrcamento(pecas: 400m, insumos: 200m);

        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(existente);

        Orcamento salvo = null!;
        _orcamentoRepositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<Orcamento>()))
            .Callback<Orcamento>(o => salvo = o)
            .ReturnsAsync((Orcamento o) => o);

        // Act
        await _sut.AtualizarManualAsync(modificado);

        // Assert
        Assert.Equal(600m, salvo.ValorTotalGeral);  // 400 + 200
    }

    [Fact]
    public async Task AtualizarManualAsync_QuandoOrcamentoNaoEncontrado_DeveLancarKeyNotFoundException()
    {
        // Arrange
        var orcamento = CriarOrcamento();

        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(orcamento.Id.ToString()))
            .ReturnsAsync((Orcamento?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.AtualizarManualAsync(orcamento));

        _orcamentoRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Orcamento>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarManualAsync_DeveAtualizarDataGeracao()
    {
        // Arrange
        var dataAnterior = DateTime.UtcNow.AddDays(-5);
        var existente    = CriarOrcamento();
        existente.DataGeracao = dataAnterior;

        var modificado = CriarOrcamento(pecas: 100m, insumos: 50m);

        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(existente);

        Orcamento salvo = null!;
        _orcamentoRepositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<Orcamento>()))
            .Callback<Orcamento>(o => salvo = o)
            .ReturnsAsync((Orcamento o) => o);

        // Act
        await _sut.AtualizarManualAsync(modificado);

        // Assert — DataGeracao deve ter sido atualizada para agora
        Assert.True(salvo.DataGeracao > dataAnterior);
    }

    [Fact]
    public async Task AtualizarManualAsync_DeveAtualizarValidade()
    {
        // Arrange
        var existente  = CriarOrcamento();
        var novaValidade = DateTime.UtcNow.AddDays(60);
        var modificado = CriarOrcamento(pecas: 100m, insumos: 50m);
        modificado.Validade = novaValidade;

        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(existente);

        Orcamento salvo = null!;
        _orcamentoRepositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<Orcamento>()))
            .Callback<Orcamento>(o => salvo = o)
            .ReturnsAsync((Orcamento o) => o);

        // Act
        await _sut.AtualizarManualAsync(modificado);

        // Assert
        Assert.Equal(novaValidade, salvo.Validade);
    }

    #endregion

    // ─── ObterPorIdAsync ────────────────────────────────────────────────────────

    #region ObterPorIdAsync

    [Fact]
    public async Task ObterPorIdAsync_QuandoEncontrado_DeveRetornarOrcamento()
    {
        // Arrange
        var orcamento = CriarOrcamento();

        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(orcamento);

        // Act
        var resultado = await _sut.ObterPorIdAsync("1");

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(orcamento.Id, resultado.Id);
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoNaoEncontrado_DeveRetornarNull()
    {
        // Arrange
        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("999"))
            .ReturnsAsync((Orcamento?)null);

        // Act
        var resultado = await _sut.ObterPorIdAsync("999");

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoRepositorioLancaExcecao_DeveRePropagar()
    {
        // Arrange
        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Erro no banco"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.ObterPorIdAsync("1"));
    }

    #endregion

    // ─── ObterPorOrdemServicoIdAsync ────────────────────────────────────────────

    #region ObterPorOrdemServicoIdAsync

    [Fact]
    public async Task ObterPorOrdemServicoIdAsync_QuandoEncontrado_DeveRetornarOrcamento()
    {
        // Arrange
        var orcamento = CriarOrcamento(ordemServicoId: 1);

        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync(orcamento);

        // Act
        var resultado = await _sut.ObterPorOrdemServicoIdAsync(1);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.OrdemServicoId);
    }

    [Fact]
    public async Task ObterPorOrdemServicoIdAsync_QuandoNaoEncontrado_DeveRetornarNull()
    {
        // Arrange
        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Orcamento?)null);

        // Act
        var resultado = await _sut.ObterPorOrdemServicoIdAsync(99);

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObterPorOrdemServicoIdAsync_QuandoRepositorioLancaExcecao_DeveRePropagar()
    {
        // Arrange
        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Erro no banco"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.ObterPorOrdemServicoIdAsync(1));
    }

    #endregion

    // ─── RemoverAsync ───────────────────────────────────────────────────────────

    #region RemoverAsync

    [Fact]
    public async Task RemoverAsync_QuandoOrcamentoExiste_DeveChamarRepositorio()
    {
        // Arrange
        var id        = "1";
        var orcamento = CriarOrcamento();

        _orcamentoRepositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(orcamento);
        _orcamentoRepositoryMock.Setup(r => r.RemoverAsync(id)).Returns(Task.CompletedTask);

        // Act
        await _sut.RemoverAsync(id);

        // Assert
        _orcamentoRepositoryMock.Verify(r => r.RemoverAsync(id), Times.Once);
    }

    [Fact]
    public async Task RemoverAsync_QuandoOrcamentoNaoEncontrado_DeveLancarKeyNotFoundException()
    {
        // Arrange
        _orcamentoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Orcamento?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.RemoverAsync("999"));

        _orcamentoRepositoryMock.Verify(r => r.RemoverAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion
}