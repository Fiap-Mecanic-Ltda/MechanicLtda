using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Domain.Interfaces.Services;
using MechanicLtda.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MechanicLtda.Domain.Tests;

public class ItemOrdemServicoServiceTests
{
    private readonly Mock<IItemOrdemServicoRepository>      _itemRepositoryMock;
    private readonly Mock<IOrdemServicoRepository>          _ordemServicoRepositoryMock;
    private readonly Mock<INotificadorService>              _notificadorMock;
    private readonly Mock<ILogger<ItemOrdemServicoService>> _loggerMock;
    private readonly Mock<IConfiguration>                  _configurationMock;
    private readonly Mock<IEstoqueService>                 _estoqueServiceMock;
    private readonly ItemOrdemServicoService               _sut;

    public ItemOrdemServicoServiceTests()
    {
        _itemRepositoryMock         = new Mock<IItemOrdemServicoRepository>();
        _ordemServicoRepositoryMock = new Mock<IOrdemServicoRepository>();
        _notificadorMock            = new Mock<INotificadorService>();
        _loggerMock                 = new Mock<ILogger<ItemOrdemServicoService>>();
        _configurationMock          = new Mock<IConfiguration>();
        _estoqueServiceMock         = new Mock<IEstoqueService>();

        _sut = new ItemOrdemServicoService(
            _loggerMock.Object,
            _configurationMock.Object,
            _notificadorMock.Object,
            _itemRepositoryMock.Object,
            _ordemServicoRepositoryMock.Object,
            _estoqueServiceMock.Object);
    }

    // ─── helpers ────────────────────────────────────────────────────────────────

    private static OrdemServico CriarOrdemServico(int id = 1, decimal? valorTotalEstimado = null) =>
        new()
        {
            Id                 = id,
            DescricaoProblema  = "Barulho no motor",
            ValorTotalEstimado = valorTotalEstimado,
            VeiculoId          = 1,
            ClienteId          = 1,
            DataCriacao        = DateTime.Now
        };

    private static ItemOrdemServico CriarItem(int id = 1, int ordemServicoId = 1,
        int quantidade = 2, decimal valorUnitario = 100m) =>
        new()
        {
            Id             = id,
            OrdemServicoId = ordemServicoId,
            EstoqueId      = null,
            Quantidade     = quantidade,
            ValorUnitario  = valorUnitario,
            ValorTotal     = quantidade * valorUnitario
        };

    private static Estoque CriarEstoque(int id = 1, int quantidadeAtual = 10, int quantidadeMinima = 2) =>
        new()
        {
            Id                    = id,
            Nome                  = "Item de Teste",
            Tipo                  = Domain.Enums.TipoEstoque.Peca,
            QuantidadeAtual       = quantidadeAtual,
            QuantidadeMinima      = quantidadeMinima,
            DataUltimaAtualizacao = DateTime.UtcNow
        };

    // Configura o mock de atualização da OS (trigger interno do service)
    private void ConfigurarTriggerAtualizacaoOS(OrdemServico ordemServico,
        IEnumerable<ItemOrdemServico> itensExistentes)
    {
        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemServico.Id.ToString()))
            .ReturnsAsync(ordemServico);

        _itemRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(ordemServico.Id))
            .ReturnsAsync(itensExistentes);

        _ordemServicoRepositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>()))
            .ReturnsAsync(ordemServico);
    }

    // ─── AdicionarAsync ─────────────────────────────────────────────────────────

    #region AdicionarAsync

    [Fact]
    public async Task AdicionarAsync_QuandoOSExiste_DeveRetornarItemComValorTotalCalculado()
    {
        // Arrange
        var ordemServico = CriarOrdemServico();
        var itemEsperado = CriarItem(quantidade: 3, valorUnitario: 50m);
        itemEsperado.ValorTotal = 150m;

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(ordemServico);

        _itemRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<ItemOrdemServico>()))
            .ReturnsAsync(itemEsperado);

        ConfigurarTriggerAtualizacaoOS(ordemServico, [itemEsperado]);

        // Act — estoqueId: null → não deve acionar subtração
        var resultado = await _sut.AdicionarAsync(
            ordemServicoId: 1, estoqueId: null, quantidade: 3, valorUnitario: 50m);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(150m, resultado.ValorTotal);
        _itemRepositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<ItemOrdemServico>()), Times.Once);
        _estoqueServiceMock.Verify(e => e.SubtrairQuantidadeAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoOSNaoEncontrada_DeveLancarKeyNotFoundException()
    {
        // Arrange
        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(It.IsAny<string>()))
            .ReturnsAsync((OrdemServico?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.AdicionarAsync(ordemServicoId: 99, estoqueId: null, quantidade: 1, valorUnitario: 10m));

        _itemRepositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<ItemOrdemServico>()), Times.Never);
        _estoqueServiceMock.Verify(e => e.SubtrairQuantidadeAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoCriado_DeveCalcularValorTotalCorretamente()
    {
        // Arrange
        var ordemServico = CriarOrdemServico();
        ItemOrdemServico itemCriado = null!;

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(ordemServico);

        _itemRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<ItemOrdemServico>()))
            .Callback<ItemOrdemServico>(i => itemCriado = i)
            .ReturnsAsync((ItemOrdemServico i) => i);

        _itemRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync([]);

        _ordemServicoRepositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>()))
            .ReturnsAsync(ordemServico);

        // Act
        await _sut.AdicionarAsync(ordemServicoId: 1, estoqueId: null, quantidade: 4, valorUnitario: 25m);

        // Assert
        Assert.NotNull(itemCriado);
        Assert.Equal(100m, itemCriado.ValorTotal);  // 4 * 25 = 100
        Assert.Equal(1, itemCriado.OrdemServicoId);
    }

    [Fact]
    public async Task AdicionarAsync_DeveTriggerAtualizacaoValorTotalEstimadoDaOS()
    {
        // Arrange
        var ordemServico   = CriarOrdemServico();
        var itemAdicionado = CriarItem(quantidade: 2, valorUnitario: 75m);
        itemAdicionado.ValorTotal = 150m;

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(ordemServico);

        _itemRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<ItemOrdemServico>()))
            .ReturnsAsync(itemAdicionado);

        _itemRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync([itemAdicionado]);

        OrdemServico osAtualizada = null!;
        _ordemServicoRepositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>()))
            .Callback<OrdemServico>(os => osAtualizada = os)
            .ReturnsAsync(ordemServico);

        // Act
        await _sut.AdicionarAsync(ordemServicoId: 1, estoqueId: null, quantidade: 2, valorUnitario: 75m);

        // Assert
        Assert.NotNull(osAtualizada);
        Assert.Equal(150m, osAtualizada.ValorTotalEstimado);
        Assert.NotNull(osAtualizada.DataModificacao);
        _ordemServicoRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemServico>()), Times.Once);
    }

    [Fact]
    public async Task AdicionarAsync_ComMultiplosItens_DeveSomarValoresNaOS()
    {
        // Arrange
        var ordemServico = CriarOrdemServico();
        var item1 = CriarItem(id: 1, quantidade: 2, valorUnitario: 100m); // 200
        var item2 = CriarItem(id: 2, quantidade: 3, valorUnitario: 50m);  // 150 — novo

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(ordemServico);

        _itemRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<ItemOrdemServico>()))
            .ReturnsAsync(item2);

        _itemRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync([item1, item2]);

        OrdemServico osAtualizada = null!;
        _ordemServicoRepositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>()))
            .Callback<OrdemServico>(os => osAtualizada = os)
            .ReturnsAsync(ordemServico);

        // Act
        await _sut.AdicionarAsync(ordemServicoId: 1, estoqueId: null, quantidade: 3, valorUnitario: 50m);

        // Assert
        Assert.Equal(350m, osAtualizada.ValorTotalEstimado); // 200 + 150
    }

    [Fact]
    public async Task AdicionarAsync_ComEstoqueId_DeveSubtrairDoEstoque()
    {
        // Arrange
        var ordemServico  = CriarOrdemServico();
        var item          = CriarItem(quantidade: 3, valorUnitario: 50m);
        var estoqueRetorno = CriarEstoque(id: 5, quantidadeAtual: 7); // 10 - 3 = 7

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(ordemServico);

        _estoqueServiceMock
            .Setup(e => e.SubtrairQuantidadeAsync(5, 3))
            .ReturnsAsync(estoqueRetorno);

        _itemRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<ItemOrdemServico>()))
            .ReturnsAsync(item);

        ConfigurarTriggerAtualizacaoOS(ordemServico, [item]);

        // Act
        var resultado = await _sut.AdicionarAsync(
            ordemServicoId: 1, estoqueId: 5, quantidade: 3, valorUnitario: 50m);

        // Assert
        Assert.NotNull(resultado);
        _estoqueServiceMock.Verify(e => e.SubtrairQuantidadeAsync(5, 3), Times.Once);
        _itemRepositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<ItemOrdemServico>()), Times.Once);
    }

    [Fact]
    public async Task AdicionarAsync_ComEstoqueId_DeveVincularEstoqueIdNoItem()
    {
        // Arrange
        var ordemServico   = CriarOrdemServico();
        var estoqueRetorno = CriarEstoque(id: 7, quantidadeAtual: 5);
        ItemOrdemServico itemCriado = null!;

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(ordemServico);

        _estoqueServiceMock
            .Setup(e => e.SubtrairQuantidadeAsync(7, 2))
            .ReturnsAsync(estoqueRetorno);

        _itemRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<ItemOrdemServico>()))
            .Callback<ItemOrdemServico>(i => itemCriado = i)
            .ReturnsAsync((ItemOrdemServico i) => i);

        _itemRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync([]);

        _ordemServicoRepositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>()))
            .ReturnsAsync(ordemServico);

        // Act
        await _sut.AdicionarAsync(ordemServicoId: 1, estoqueId: 7, quantidade: 2, valorUnitario: 100m);

        // Assert
        Assert.NotNull(itemCriado);
        Assert.Equal(7, itemCriado.EstoqueId);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoEstoqueSemSaldo_DeveLancarInvalidOperationException()
    {
        // Arrange
        var ordemServico = CriarOrdemServico();

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(ordemServico);

        _estoqueServiceMock
            .Setup(e => e.SubtrairQuantidadeAsync(3, 10))
            .ThrowsAsync(new InvalidOperationException("Saldo insuficiente no estoque."));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AdicionarAsync(ordemServicoId: 1, estoqueId: 3, quantidade: 10, valorUnitario: 50m));

        _itemRepositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<ItemOrdemServico>()), Times.Never);
    }

    [Fact]
    public async Task AdicionarAsync_SemEstoqueId_NaoDeveAcionarSubtracao()
    {
        // Arrange
        var ordemServico = CriarOrdemServico();
        var item         = CriarItem();

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(ordemServico);

        _itemRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<ItemOrdemServico>()))
            .ReturnsAsync(item);

        ConfigurarTriggerAtualizacaoOS(ordemServico, [item]);

        // Act
        await _sut.AdicionarAsync(ordemServicoId: 1, estoqueId: null, quantidade: 2, valorUnitario: 100m);

        // Assert
        _estoqueServiceMock.Verify(
            e => e.SubtrairQuantidadeAsync(It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
    }

    #endregion

    // ─── AtualizarAsync ─────────────────────────────────────────────────────────

    #region AtualizarAsync

    [Fact]
    public async Task AtualizarAsync_QuandoItemExiste_DeveRecalcularValorTotal()
    {
        // Arrange
        var itemExistente  = CriarItem(quantidade: 2, valorUnitario: 50m);
        var ordemServico   = CriarOrdemServico();
        var itemAtualizado = CriarItem(quantidade: 5, valorUnitario: 80m);

        _itemRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(itemExistente);

        _itemRepositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<ItemOrdemServico>()))
            .ReturnsAsync(itemAtualizado);

        ConfigurarTriggerAtualizacaoOS(ordemServico, [itemAtualizado]);

        // Act
        var resultado = await _sut.AtualizarAsync(itemAtualizado);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(400m, resultado.ValorTotal); // 5 * 80
        _itemRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<ItemOrdemServico>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoItemNaoEncontrado_DeveLancarKeyNotFoundException()
    {
        // Arrange
        var item = CriarItem();

        _itemRepositoryMock
            .Setup(r => r.ObterPorIdAsync(item.Id.ToString()))
            .ReturnsAsync((ItemOrdemServico?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.AtualizarAsync(item));

        _itemRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<ItemOrdemServico>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarAsync_DevePreservarOrdemServicoIdOriginal()
    {
        // Arrange
        var itemExistente  = CriarItem(ordemServicoId: 1);
        var ordemServico   = CriarOrdemServico(id: 1);
        ItemOrdemServico itemSalvo = null!;

        // Tenta alterar OrdemServicoId para 99 — deve ser ignorado
        var itemModificado = CriarItem(ordemServicoId: 99);

        _itemRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(itemExistente);

        _itemRepositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<ItemOrdemServico>()))
            .Callback<ItemOrdemServico>(i => itemSalvo = i)
            .ReturnsAsync((ItemOrdemServico i) => i);

        ConfigurarTriggerAtualizacaoOS(ordemServico, [itemModificado]);

        // Act
        await _sut.AtualizarAsync(itemModificado);

        // Assert
        Assert.Equal(1, itemSalvo.OrdemServicoId);
    }

    [Fact]
    public async Task AtualizarAsync_DeveTriggerAtualizacaoValorTotalEstimadoDaOS()
    {
        // Arrange
        var itemExistente  = CriarItem(quantidade: 1, valorUnitario: 100m);
        var itemAtualizado = CriarItem(quantidade: 3, valorUnitario: 200m);
        itemAtualizado.ValorTotal = 600m;
        var ordemServico   = CriarOrdemServico();

        _itemRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(itemExistente);

        _itemRepositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<ItemOrdemServico>()))
            .ReturnsAsync(itemAtualizado);

        OrdemServico osAtualizada = null!;
        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(ordemServico);

        _itemRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync([itemAtualizado]);

        _ordemServicoRepositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>()))
            .Callback<OrdemServico>(os => osAtualizada = os)
            .ReturnsAsync(ordemServico);

        // Act
        await _sut.AtualizarAsync(itemAtualizado);

        // Assert
        Assert.NotNull(osAtualizada);
        Assert.Equal(600m, osAtualizada.ValorTotalEstimado);
        Assert.NotNull(osAtualizada.DataModificacao);
    }

    #endregion

    // ─── RemoverAsync ───────────────────────────────────────────────────────────

    #region RemoverAsync

    [Fact]
    public async Task RemoverAsync_QuandoItemExiste_DeveChamarRepositorio()
    {
        // Arrange
        var id           = "1";
        var item         = CriarItem();
        var ordemServico = CriarOrdemServico();

        _itemRepositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(item);
        _itemRepositoryMock.Setup(r => r.RemoverAsync(id)).Returns(Task.CompletedTask);
        ConfigurarTriggerAtualizacaoOS(ordemServico, []);

        // Act
        await _sut.RemoverAsync(id);

        // Assert
        _itemRepositoryMock.Verify(r => r.RemoverAsync(id), Times.Once);
    }

    [Fact]
    public async Task RemoverAsync_QuandoItemNaoEncontrado_DeveLancarKeyNotFoundException()
    {
        // Arrange
        _itemRepositoryMock
            .Setup(r => r.ObterPorIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ItemOrdemServico?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.RemoverAsync("999"));

        _itemRepositoryMock.Verify(r => r.RemoverAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RemoverAsync_DeveTriggerAtualizacaoValorTotalEstimadoDaOS()
    {
        // Arrange
        var id           = "1";
        var item         = CriarItem(quantidade: 2, valorUnitario: 100m);  // 200
        var ordemServico = CriarOrdemServico(valorTotalEstimado: 200m);

        _itemRepositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(item);
        _itemRepositoryMock.Setup(r => r.RemoverAsync(id)).Returns(Task.CompletedTask);

        // Após remoção, a OS não tem mais itens
        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(ordemServico);

        _itemRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(1))
            .ReturnsAsync([]);

        OrdemServico osAtualizada = null!;
        _ordemServicoRepositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>()))
            .Callback<OrdemServico>(os => osAtualizada = os)
            .ReturnsAsync(ordemServico);

        // Act
        await _sut.RemoverAsync(id);

        // Assert
        Assert.NotNull(osAtualizada);
        Assert.Equal(0m, osAtualizada.ValorTotalEstimado);  // sem itens → 0
        Assert.NotNull(osAtualizada.DataModificacao);
        _ordemServicoRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemServico>()), Times.Once);
    }

    #endregion

    // ─── ObterPorOrdemServicoIdAsync ────────────────────────────────────────────

    #region ObterPorOrdemServicoIdAsync

    [Fact]
    public async Task ObterPorOrdemServicoIdAsync_DeveRetornarItensCorretos()
    {
        // Arrange
        var ordemServicoId = 1;
        var lista = new List<ItemOrdemServico>
        {
            CriarItem(id: 1, ordemServicoId: ordemServicoId),
            CriarItem(id: 2, ordemServicoId: ordemServicoId)
        };

        _itemRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(ordemServicoId))
            .ReturnsAsync(lista);

        // Act
        var resultado = await _sut.ObterPorOrdemServicoIdAsync(ordemServicoId);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(2, resultado.Count());
        Assert.All(resultado, i => Assert.Equal(ordemServicoId, i.OrdemServicoId));
    }

    [Fact]
    public async Task ObterPorOrdemServicoIdAsync_QuandoRepositorioLancaExcecao_DeveRePropagar()
    {
        // Arrange
        _itemRepositoryMock
            .Setup(r => r.ObterPorOrdemServicoIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Erro no banco"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _sut.ObterPorOrdemServicoIdAsync(1));
    }

    #endregion

    // ─── ObterPorIdAsync ────────────────────────────────────────────────────────

    #region ObterPorIdAsync

    [Fact]
    public async Task ObterPorIdAsync_QuandoItemEncontrado_DeveRetornarItem()
    {
        // Arrange
        var item = CriarItem();

        _itemRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(item);

        // Act
        var resultado = await _sut.ObterPorIdAsync("1");

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(item.Id, resultado.Id);
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoNaoEncontrado_DeveRetornarNull()
    {
        // Arrange
        _itemRepositoryMock
            .Setup(r => r.ObterPorIdAsync("999"))
            .ReturnsAsync((ItemOrdemServico?)null);

        // Act
        var resultado = await _sut.ObterPorIdAsync("999");

        // Assert
        Assert.Null(resultado);
    }

    #endregion

    // ─── CalcularValorTotal (entidade) ──────────────────────────────────────────

    #region CalcularValorTotal

    [Theory]
    [InlineData(1, 100.00, 100.00)]
    [InlineData(3, 50.00,  150.00)]
    [InlineData(10, 9.99,   99.90)]
    public void CalcularValorTotal_DeveMultiplicarQuantidadePorValorUnitario(
        int quantidade, decimal valorUnitario, decimal valorEsperado)
    {
        // Arrange
        var item = new ItemOrdemServico
        {
            Quantidade    = quantidade,
            ValorUnitario = valorUnitario
        };

        // Act
        item.CalcularValorTotal();

        // Assert
        Assert.Equal(valorEsperado, item.ValorTotal);
    }

    #endregion
}