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

public class EstoqueServiceTests
{
    private readonly Mock<IEstoqueRepository>          _repositoryMock;
    private readonly Mock<INotificadorService>         _notificadorMock;
    private readonly Mock<ILogger<EstoqueService>>     _loggerMock;
    private readonly Mock<IConfiguration>             _configurationMock;
    private readonly EstoqueService                   _sut;

    public EstoqueServiceTests()
    {
        _repositoryMock    = new Mock<IEstoqueRepository>();
        _notificadorMock   = new Mock<INotificadorService>();
        _loggerMock        = new Mock<ILogger<EstoqueService>>();
        _configurationMock = new Mock<IConfiguration>();

        _sut = new EstoqueService(
            _loggerMock.Object,
            _configurationMock.Object,
            _notificadorMock.Object,
            _repositoryMock.Object);
    }

    // ─── helpers ────────────────────────────────────────────────────────────────

    private static Estoque CriarEstoque(
        int id               = 1,
        string nome          = "Filtro de Óleo",
        TipoEstoque tipo     = TipoEstoque.Peca,
        int quantidadeAtual  = 10,
        int quantidadeMinima = 2) =>
        new()
        {
            Id                    = id,
            Nome                  = nome,
            Tipo                  = tipo,
            QuantidadeAtual       = quantidadeAtual,
            QuantidadeMinima      = quantidadeMinima,
            DataUltimaAtualizacao = DateTime.UtcNow
        };

    // ─── AdicionarAsync ─────────────────────────────────────────────────────────

    #region AdicionarAsync

    [Fact]
    public async Task AdicionarAsync_QuandoValido_DeveRetornarEstoqueCriado()
    {
        // Arrange
        var estoque = CriarEstoque();

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Estoque>()))
            .ReturnsAsync(estoque);

        // Act
        var resultado = await _sut.AdicionarAsync(estoque);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(estoque.Nome, resultado.Nome);
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Estoque>()), Times.Once);
    }

    [Fact]
    public async Task AdicionarAsync_DeveDefinirDataUltimaAtualizacao()
    {
        // Arrange
        var estoque = CriarEstoque();
        Estoque estoqueSalvo = null!;

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Estoque>()))
            .Callback<Estoque>(e => estoqueSalvo = e)
            .ReturnsAsync(estoque);

        // Act
        await _sut.AdicionarAsync(estoque);

        // Assert
        Assert.NotEqual(default, estoqueSalvo.DataUltimaAtualizacao);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoRepositorioLancaExcecao_DeveRePropagar()
    {
        // Arrange
        var estoque = CriarEstoque();

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Estoque>()))
            .ThrowsAsync(new Exception("Erro no banco"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.AdicionarAsync(estoque));
    }

    #endregion

    // ─── AtualizarAsync ─────────────────────────────────────────────────────────

    #region AtualizarAsync

    [Fact]
    public async Task AtualizarAsync_QuandoEstoqueExiste_DeveRetornarEstoqueAtualizado()
    {
        // Arrange
        var existente   = CriarEstoque();
        var atualizado  = CriarEstoque(nome: "Filtro de Ar", quantidadeMinima: 5);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(existente);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<Estoque>()))
            .ReturnsAsync(atualizado);

        // Act
        var resultado = await _sut.AtualizarAsync(atualizado);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal("Filtro de Ar", resultado.Nome);
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Estoque>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoEstoqueNaoEncontrado_DeveLancarKeyNotFoundException()
    {
        // Arrange
        var estoque = CriarEstoque();

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(estoque.Id.ToString()))
            .ReturnsAsync((Estoque?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.AtualizarAsync(estoque));

        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Estoque>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarAsync_DeveAtualizarDataUltimaAtualizacao()
    {
        // Arrange
        var existente  = CriarEstoque();
        var atualizado = CriarEstoque();
        Estoque estoqueSalvo = null!;

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(existente);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<Estoque>()))
            .Callback<Estoque>(e => estoqueSalvo = e)
            .ReturnsAsync(atualizado);

        // Act
        await _sut.AtualizarAsync(atualizado);

        // Assert
        Assert.NotEqual(default, estoqueSalvo.DataUltimaAtualizacao);
    }

    #endregion

    // ─── RemoverAsync ───────────────────────────────────────────────────────────

    #region RemoverAsync

    [Fact]
    public async Task RemoverAsync_QuandoEstoqueExiste_DeveChamarRepositorio()
    {
        // Arrange
        var id      = "1";
        var estoque = CriarEstoque();

        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(estoque);
        _repositoryMock.Setup(r => r.RemoverAsync(id)).Returns(Task.CompletedTask);

        // Act
        await _sut.RemoverAsync(id);

        // Assert
        _repositoryMock.Verify(r => r.RemoverAsync(id), Times.Once);
    }

    [Fact]
    public async Task RemoverAsync_QuandoEstoqueNaoEncontrado_DeveLancarKeyNotFoundException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Estoque?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.RemoverAsync("99"));

        _repositoryMock.Verify(r => r.RemoverAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion

    // ─── ObterTodosAsync ────────────────────────────────────────────────────────

    #region ObterTodosAsync

    [Fact]
    public async Task ObterTodosAsync_DeveRetornarTodosOsItens()
    {
        // Arrange
        var lista = new List<Estoque>
        {
            CriarEstoque(id: 1, nome: "Filtro de Óleo"),
            CriarEstoque(id: 2, nome: "Pastilha de Freio", tipo: TipoEstoque.Peca),
            CriarEstoque(id: 3, nome: "Óleo de Motor",     tipo: TipoEstoque.Insumo)
        };

        _repositoryMock.Setup(r => r.ObterTodosAsync()).ReturnsAsync(lista);

        // Act
        var resultado = await _sut.ObterTodosAsync();

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(3, resultado.Count());
    }

    [Fact]
    public async Task ObterTodosAsync_QuandoRepositorioLancaExcecao_DeveRePropagar()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ObterTodosAsync())
            .ThrowsAsync(new Exception("Erro no banco"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.ObterTodosAsync());
    }

    #endregion

    // ─── ObterPorTipoAsync ──────────────────────────────────────────────────────

    #region ObterPorTipoAsync

    [Fact]
    public async Task ObterPorTipoAsync_DeveRetornarApenasItensDeTipoPeca()
    {
        // Arrange
        var lista = new List<Estoque>
        {
            CriarEstoque(id: 1, tipo: TipoEstoque.Peca),
            CriarEstoque(id: 2, tipo: TipoEstoque.Peca)
        };

        _repositoryMock
            .Setup(r => r.ObterPorTipoAsync(TipoEstoque.Peca))
            .ReturnsAsync(lista);

        // Act
        var resultado = await _sut.ObterPorTipoAsync(TipoEstoque.Peca);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(2, resultado.Count());
        Assert.All(resultado, e => Assert.Equal(TipoEstoque.Peca, e.Tipo));
    }

    [Fact]
    public async Task ObterPorTipoAsync_DeveRetornarApenasItensDeTipoInsumo()
    {
        // Arrange
        var lista = new List<Estoque>
        {
            CriarEstoque(id: 3, tipo: TipoEstoque.Insumo)
        };

        _repositoryMock
            .Setup(r => r.ObterPorTipoAsync(TipoEstoque.Insumo))
            .ReturnsAsync(lista);

        // Act
        var resultado = await _sut.ObterPorTipoAsync(TipoEstoque.Insumo);

        // Assert
        Assert.Single(resultado);
        Assert.All(resultado, e => Assert.Equal(TipoEstoque.Insumo, e.Tipo));
    }

    #endregion

    // ─── ObterPorIdAsync ────────────────────────────────────────────────────────

    #region ObterPorIdAsync

    [Fact]
    public async Task ObterPorIdAsync_QuandoEncontrado_DeveRetornarEstoque()
    {
        // Arrange
        var estoque = CriarEstoque();

        _repositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(estoque);

        // Act
        var resultado = await _sut.ObterPorIdAsync("1");

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(estoque.Id, resultado.Id);
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoNaoEncontrado_DeveRetornarNull()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ObterPorIdAsync("999"))
            .ReturnsAsync((Estoque?)null);

        // Act
        var resultado = await _sut.ObterPorIdAsync("999");

        // Assert
        Assert.Null(resultado);
    }

    #endregion

    // ─── SubtrairQuantidadeAsync ─────────────────────────────────────────────────

    #region SubtrairQuantidadeAsync

    [Fact]
    public async Task SubtrairQuantidadeAsync_QuandoHaSaldo_DeveSubtrairCorretamente()
    {
        // Arrange
        var estoque   = CriarEstoque(quantidadeAtual: 10, quantidadeMinima: 2);
        var esperado  = CriarEstoque(quantidadeAtual: 7,  quantidadeMinima: 2);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(estoque);

        Estoque estoqueSalvo = null!;
        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<Estoque>()))
            .Callback<Estoque>(e => estoqueSalvo = e)
            .ReturnsAsync(esperado);

        // Act
        var resultado = await _sut.SubtrairQuantidadeAsync(estoqueId: 1, quantidade: 3);

        // Assert
        Assert.Equal(7, estoqueSalvo.QuantidadeAtual);
        Assert.Equal(7, resultado.QuantidadeAtual);
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Estoque>()), Times.Once);
    }

    [Fact]
    public async Task SubtrairQuantidadeAsync_QuandoSaldoInsuficiente_DeveLancarInvalidOperationException()
    {
        // Arrange
        var estoque = CriarEstoque(quantidadeAtual: 2);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(estoque);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.SubtrairQuantidadeAsync(estoqueId: 1, quantidade: 5));

        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Estoque>()), Times.Never);
    }

    [Fact]
    public async Task SubtrairQuantidadeAsync_QuandoEstoqueNaoExiste_DeveCriarRegistroAutomaticoComQuantidadeZero()
    {
        // Arrange
        var estoqueId = 99;
        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(estoqueId.ToString()))
            .ReturnsAsync((Estoque?)null);

        Estoque estoqueCriado = null!;
        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Estoque>()))
            .Callback<Estoque>(e => estoqueCriado = e)
            .ReturnsAsync((Estoque e) => e);

        // Act
        var resultado = await _sut.SubtrairQuantidadeAsync(estoqueId, quantidade: 3);

        // Assert
        Assert.NotNull(estoqueCriado);
        Assert.Equal(0, estoqueCriado.QuantidadeAtual);
        Assert.Equal(estoqueId, estoqueCriado.Id);
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Estoque>()), Times.Once);
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Estoque>()), Times.Never);
    }

    [Fact]
    public async Task SubtrairQuantidadeAsync_QuandoAtingeQuantidadeMinima_DeveNotificar()
    {
        // Arrange — após subtração, QuantidadeAtual == QuantidadeMinima (nível crítico)
        var estoque  = CriarEstoque(quantidadeAtual: 5, quantidadeMinima: 3);
        var esperado = CriarEstoque(quantidadeAtual: 3, quantidadeMinima: 3);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(estoque);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<Estoque>()))
            .ReturnsAsync(esperado);

        // Act
        await _sut.SubtrairQuantidadeAsync(estoqueId: 1, quantidade: 2);

        // Assert — notificador deve ter sido acionado
        _notificadorMock.Verify(
            n => n.Handle(It.IsAny<Notificacao>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SubtrairQuantidadeAsync_QuandoAtingeZero_DeveNotificarAlertaCritico()
    {
        // Arrange
        var estoque  = CriarEstoque(quantidadeAtual: 3, quantidadeMinima: 1);
        var esperado = CriarEstoque(quantidadeAtual: 0, quantidadeMinima: 1);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(estoque);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<Estoque>()))
            .ReturnsAsync(esperado);

        // Act
        await _sut.SubtrairQuantidadeAsync(estoqueId: 1, quantidade: 3);

        // Assert
        _notificadorMock.Verify(
            n => n.Handle(It.IsAny<Notificacao>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SubtrairQuantidadeAsync_QuandoAindaHaSaldoAcimaDaMinima_NaoDeveNotificar()
    {
        // Arrange — subtrai mas ainda fica acima da mínima
        var estoque  = CriarEstoque(quantidadeAtual: 10, quantidadeMinima: 2);
        var esperado = CriarEstoque(quantidadeAtual: 5,  quantidadeMinima: 2);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(estoque);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<Estoque>()))
            .ReturnsAsync(esperado);

        // Act
        await _sut.SubtrairQuantidadeAsync(estoqueId: 1, quantidade: 5);

        // Assert — nenhuma notificação de baixo estoque deve ser disparada
        _notificadorMock.Verify(
            n => n.Handle(It.IsAny<Notificacao>()),
            Times.Never);
    }

    [Fact]
    public async Task SubtrairQuantidadeAsync_DeveAtualizarDataUltimaAtualizacao()
    {
        // Arrange
        var estoque  = CriarEstoque(quantidadeAtual: 10);
        var esperado = CriarEstoque(quantidadeAtual: 8);
        Estoque estoqueSalvo = null!;

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(estoque);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<Estoque>()))
            .Callback<Estoque>(e => estoqueSalvo = e)
            .ReturnsAsync(esperado);

        // Act
        await _sut.SubtrairQuantidadeAsync(estoqueId: 1, quantidade: 2);

        // Assert
        Assert.NotEqual(default, estoqueSalvo.DataUltimaAtualizacao);
    }

    #endregion

    // ─── ReporQuantidadeAsync ────────────────────────────────────────────────────

    #region ReporQuantidadeAsync

    [Fact]
    public async Task ReporQuantidadeAsync_QuandoValido_DeveAdicionarQuantidade()
    {
        // Arrange
        var estoque  = CriarEstoque(quantidadeAtual: 5);
        var esperado = CriarEstoque(quantidadeAtual: 15);
        Estoque estoqueSalvo = null!;

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(estoque);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<Estoque>()))
            .Callback<Estoque>(e => estoqueSalvo = e)
            .ReturnsAsync(esperado);

        // Act
        var resultado = await _sut.ReporQuantidadeAsync(estoqueId: 1, quantidadeEntrada: 10);

        // Assert
        Assert.Equal(15, estoqueSalvo.QuantidadeAtual);  // 5 + 10
        Assert.Equal(15, resultado.QuantidadeAtual);
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Estoque>()), Times.Once);
    }

    [Fact]
    public async Task ReporQuantidadeAsync_QuandoQuantidadeZeroOuNegativa_DeveLancarArgumentException()
    {
        // Act & Assert — zero
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.ReporQuantidadeAsync(estoqueId: 1, quantidadeEntrada: 0));

        // Act & Assert — negativo
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.ReporQuantidadeAsync(estoqueId: 1, quantidadeEntrada: -5));

        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Estoque>()), Times.Never);
    }

    [Fact]
    public async Task ReporQuantidadeAsync_QuandoEstoqueNaoEncontrado_DeveLancarKeyNotFoundException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Estoque?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.ReporQuantidadeAsync(estoqueId: 99, quantidadeEntrada: 10));

        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Estoque>()), Times.Never);
    }

    [Fact]
    public async Task ReporQuantidadeAsync_DeveAtualizarDataUltimaAtualizacao()
    {
        // Arrange
        var estoque  = CriarEstoque(quantidadeAtual: 3);
        var esperado = CriarEstoque(quantidadeAtual: 13);
        Estoque estoqueSalvo = null!;

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(estoque);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<Estoque>()))
            .Callback<Estoque>(e => estoqueSalvo = e)
            .ReturnsAsync(esperado);

        // Act
        await _sut.ReporQuantidadeAsync(estoqueId: 1, quantidadeEntrada: 10);

        // Assert
        Assert.NotEqual(default, estoqueSalvo.DataUltimaAtualizacao);
    }

    #endregion

    // ─── EstaBaixoEstoque (entidade) ─────────────────────────────────────────────

    #region EstaBaixoEstoque

    [Theory]
    [InlineData(0,  2, true)]   // sem estoque
    [InlineData(2,  2, true)]   // igual ao mínimo
    [InlineData(1,  2, true)]   // abaixo do mínimo
    [InlineData(3,  2, false)]  // acima do mínimo
    [InlineData(10, 5, false)]  // bem acima do mínimo
    public void EstaBaixoEstoque_DeveRetornarCorretamente(
        int quantidadeAtual, int quantidadeMinima, bool esperado)
    {
        // Arrange
        var estoque = CriarEstoque(
            quantidadeAtual:  quantidadeAtual,
            quantidadeMinima: quantidadeMinima);

        // Act
        var resultado = estoque.EstaBaixoEstoque();

        // Assert
        Assert.Equal(esperado, resultado);
    }

    #endregion
}