using AutoMapper;
using MechanicLtda.Application.AppServices;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;
using MechanicLtda.Domain.Interfaces.Services;
using Moq;
using Xunit;

namespace MechanicLtda.Application.Tests;

public class EstoqueAppServiceTests
{
    private readonly Mock<IEstoqueService> _serviceMock;
    private readonly Mock<IMapper>         _mapperMock;
    private readonly EstoqueAppService     _sut;

    public EstoqueAppServiceTests()
    {
        _serviceMock = new Mock<IEstoqueService>();
        _mapperMock  = new Mock<IMapper>();
        _sut = new EstoqueAppService(_serviceMock.Object, _mapperMock.Object);
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

    private static EstoqueDto CriarDto(
        int id               = 1,
        string nome          = "Filtro de Óleo",
        TipoEstoque tipo     = TipoEstoque.Peca,
        int quantidadeAtual  = 10,
        int quantidadeMinima = 2,
        bool baixoEstoque    = false) =>
        new()
        {
            Id                    = id,
            Nome                  = nome,
            Tipo                  = tipo,
            QuantidadeAtual       = quantidadeAtual,
            QuantidadeMinima      = quantidadeMinima,
            DataUltimaAtualizacao = DateTime.UtcNow,
            BaixoEstoque          = baixoEstoque
        };

    // ─── AdicionarAsync ─────────────────────────────────────────────────────────

    #region AdicionarAsync

    [Fact]
    public async Task AdicionarAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var createDto = new EstoqueCreateDto { Nome = "Filtro de Óleo", Tipo = TipoEstoque.Peca, QuantidadeAtual = 10, QuantidadeMinima = 2 };
        var entidade  = CriarEstoque();
        var dto       = CriarDto();

        _mapperMock.Setup(m => m.Map<Estoque>(createDto)).Returns(entidade);
        _serviceMock.Setup(s => s.AdicionarAsync(entidade)).ReturnsAsync(entidade);

        // Act
        var response = await _sut.AdicionarAsync(createDto);

        // Assert
        Assert.False(response.hasErrors);
        Assert.NotNull(response.getResponse);
        Assert.Equal("Filtro de Óleo", response.getResponse.Nome);
        _serviceMock.Verify(s => s.AdicionarAsync(entidade), Times.Once);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoServicoLancaExcecao_DeveRetornarResponseComErro()
    {
        // Arrange
        var createDto = new EstoqueCreateDto { Nome = "Filtro", Tipo = TipoEstoque.Insumo };
        var entidade  = CriarEstoque();

        _mapperMock.Setup(m => m.Map<Estoque>(createDto)).Returns(entidade);
        _serviceMock
            .Setup(s => s.AdicionarAsync(It.IsAny<Estoque>()))
            .ThrowsAsync(new Exception("Erro inesperado."));

        // Act
        var response = await _sut.AdicionarAsync(createDto);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    // ─── AtualizarAsync ─────────────────────────────────────────────────────────

    #region AtualizarAsync

    [Fact]
    public async Task AtualizarAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var updateDto = new EstoqueUpdateDto { Nome = "Filtro de Ar", Tipo = TipoEstoque.Peca, QuantidadeMinima = 3 };
        var entidade  = CriarEstoque(nome: "Filtro de Ar", quantidadeMinima: 3);

        _mapperMock.Setup(m => m.Map<Estoque>(updateDto)).Returns(entidade);
        _serviceMock.Setup(s => s.AtualizarAsync(It.IsAny<Estoque>())).ReturnsAsync(entidade);

        // Act
        var response = await _sut.AtualizarAsync("1", updateDto);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal("Filtro de Ar", response.getResponse.Nome);
        _serviceMock.Verify(s => s.AtualizarAsync(It.IsAny<Estoque>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoEstoqueNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        var updateDto = new EstoqueUpdateDto { Nome = "X", Tipo = TipoEstoque.Peca };

        _mapperMock.Setup(m => m.Map<Estoque>(updateDto)).Returns(new Estoque());
        _serviceMock
            .Setup(s => s.AtualizarAsync(It.IsAny<Estoque>()))
            .ThrowsAsync(new KeyNotFoundException("Estoque com Id '99' não encontrado."));

        // Act
        var response = await _sut.AtualizarAsync("99", updateDto);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    [Fact]
    public async Task AtualizarAsync_DeveAtribuirIdCorretoNaEntidade()
    {
        // Arrange
        var updateDto = new EstoqueUpdateDto { Nome = "Óleo", Tipo = TipoEstoque.Insumo, QuantidadeMinima = 1 };
        Estoque entidadeSalva = null!;

        _mapperMock.Setup(m => m.Map<Estoque>(updateDto)).Returns(new Estoque());
        _serviceMock
            .Setup(s => s.AtualizarAsync(It.IsAny<Estoque>()))
            .Callback<Estoque>(e => entidadeSalva = e)
            .ReturnsAsync(CriarEstoque(id: 5));

        // Act
        await _sut.AtualizarAsync("5", updateDto);

        // Assert
        Assert.Equal(5, entidadeSalva.Id);
    }

    #endregion

    // ─── RemoverAsync ───────────────────────────────────────────────────────────

    #region RemoverAsync

    [Fact]
    public async Task RemoverAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var id = "1";

        _serviceMock.Setup(s => s.RemoverAsync(id)).Returns(Task.CompletedTask);

        // Act
        var response = await _sut.RemoverAsync(id);

        // Assert
        Assert.False(response.hasErrors);
        Assert.True(response.getResponse);
        _serviceMock.Verify(s => s.RemoverAsync(id), Times.Once);
    }

    [Fact]
    public async Task RemoverAsync_QuandoEstoqueNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.RemoverAsync("99"))
            .ThrowsAsync(new KeyNotFoundException("Estoque com Id '99' não encontrado."));

        // Act
        var response = await _sut.RemoverAsync("99");

        // Assert
        Assert.True(response.hasErrors);
        Assert.False(response.getResponse);
    }

    [Fact]
    public async Task RemoverAsync_QuandoServicoLancaExcecaoGenerica_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.RemoverAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Erro inesperado."));

        // Act
        var response = await _sut.RemoverAsync("1");

        // Assert
        Assert.True(response.hasErrors);
        Assert.False(response.getResponse);
    }

    #endregion

    // ─── ObterTodosAsync ────────────────────────────────────────────────────────

    #region ObterTodosAsync

    [Fact]
    public async Task ObterTodosAsync_QuandoSucesso_DeveRetornarTodosOsItens()
    {
        // Arrange
        var lista = new List<Estoque>
        {
            CriarEstoque(id: 1),
            CriarEstoque(id: 2, tipo: TipoEstoque.Insumo)
        };

        _serviceMock.Setup(s => s.ObterTodosAsync()).ReturnsAsync(lista);

        // Act
        var response = await _sut.ObterTodosAsync();

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(2, response.getResponse.Count());
    }

    [Fact]
    public async Task ObterTodosAsync_QuandoServicoLancaExcecao_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.ObterTodosAsync())
            .ThrowsAsync(new Exception("Erro no banco"));

        // Act
        var response = await _sut.ObterTodosAsync();

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    // ─── ObterPorTipoAsync ──────────────────────────────────────────────────────

    #region ObterPorTipoAsync

    [Fact]
    public async Task ObterPorTipoAsync_QuandoSucesso_DeveRetornarApenasDoTipoSolicitado()
    {
        // Arrange
        var lista = new List<Estoque>
        {
            CriarEstoque(id: 1, tipo: TipoEstoque.Peca),
            CriarEstoque(id: 2, tipo: TipoEstoque.Peca)
        };

        _serviceMock.Setup(s => s.ObterPorTipoAsync(TipoEstoque.Peca)).ReturnsAsync(lista);

        // Act
        var response = await _sut.ObterPorTipoAsync(TipoEstoque.Peca);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(2, response.getResponse.Count());
        Assert.All(response.getResponse, d => Assert.Equal(TipoEstoque.Peca, d.Tipo));
    }

    [Fact]
    public async Task ObterPorTipoAsync_QuandoServicoLancaExcecao_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.ObterPorTipoAsync(It.IsAny<TipoEstoque>()))
            .ThrowsAsync(new Exception("Erro no banco"));

        // Act
        var response = await _sut.ObterPorTipoAsync(TipoEstoque.Insumo);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    // ─── ObterPorIdAsync ────────────────────────────────────────────────────────

    #region ObterPorIdAsync

    [Fact]
    public async Task ObterPorIdAsync_QuandoEncontrado_DeveRetornarResponseSemErros()
    {
        // Arrange
        var estoque = CriarEstoque();

        _serviceMock.Setup(s => s.ObterPorIdAsync("1")).ReturnsAsync(estoque);

        // Act
        var response = await _sut.ObterPorIdAsync("1");

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(1, response.getResponse.Id);
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock.Setup(s => s.ObterPorIdAsync("999")).ReturnsAsync((Estoque?)null);

        // Act
        var response = await _sut.ObterPorIdAsync("999");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    // ─── ReporQuantidadeAsync ────────────────────────────────────────────────────

    #region ReporQuantidadeAsync

    [Fact]
    public async Task ReporQuantidadeAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var reposicaoDto = new EstoqueReposicaoDto { QuantidadeEntrada = 10 };
        var esperado     = CriarEstoque(quantidadeAtual: 20);

        _serviceMock.Setup(s => s.ReporQuantidadeAsync(1, 10)).ReturnsAsync(esperado);

        // Act
        var response = await _sut.ReporQuantidadeAsync("1", reposicaoDto);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(20, response.getResponse.QuantidadeAtual);
        _serviceMock.Verify(s => s.ReporQuantidadeAsync(1, 10), Times.Once);
    }

    [Fact]
    public async Task ReporQuantidadeAsync_QuandoEstoqueNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        var reposicaoDto = new EstoqueReposicaoDto { QuantidadeEntrada = 5 };

        _serviceMock
            .Setup(s => s.ReporQuantidadeAsync(99, 5))
            .ThrowsAsync(new KeyNotFoundException("Estoque com Id '99' não encontrado."));

        // Act
        var response = await _sut.ReporQuantidadeAsync("99", reposicaoDto);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    [Fact]
    public async Task ReporQuantidadeAsync_QuandoQuantidadeInvalida_DeveRetornarResponseComErro()
    {
        // Arrange
        var reposicaoDto = new EstoqueReposicaoDto { QuantidadeEntrada = 0 };

        _serviceMock
            .Setup(s => s.ReporQuantidadeAsync(It.IsAny<int>(), 0))
            .ThrowsAsync(new ArgumentException("A quantidade de entrada deve ser maior que zero."));

        // Act
        var response = await _sut.ReporQuantidadeAsync("1", reposicaoDto);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    [Fact]
    public async Task ReporQuantidadeAsync_QuandoServicoLancaExcecaoGenerica_DeveRetornarResponseComErro()
    {
        // Arrange
        var reposicaoDto = new EstoqueReposicaoDto { QuantidadeEntrada = 5 };

        _serviceMock
            .Setup(s => s.ReporQuantidadeAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Erro inesperado."));

        // Act
        var response = await _sut.ReporQuantidadeAsync("1", reposicaoDto);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    // ─── BaixoEstoque (campo calculado no DTO) ───────────────────────────────────

    #region BaixoEstoque

    [Fact]
    public async Task ObterPorIdAsync_QuandoEstoqueAbaixoDaMinima_DeveRetornarBaixoEstoqueTrue()
    {
        // Arrange — quantidadeAtual (1) <= quantidadeMinima (2) → BaixoEstoque = true
        var estoque = CriarEstoque(quantidadeAtual: 1, quantidadeMinima: 2);

        _serviceMock.Setup(s => s.ObterPorIdAsync("1")).ReturnsAsync(estoque);

        // Act
        var response = await _sut.ObterPorIdAsync("1");

        // Assert
        Assert.True(response.getResponse.BaixoEstoque);
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoEstoqueAcimaDaMinima_DeveRetornarBaixoEstoqueFalse()
    {
        // Arrange — quantidadeAtual (10) > quantidadeMinima (2) → BaixoEstoque = false
        var estoque = CriarEstoque(quantidadeAtual: 10, quantidadeMinima: 2);

        _serviceMock.Setup(s => s.ObterPorIdAsync("1")).ReturnsAsync(estoque);

        // Act
        var response = await _sut.ObterPorIdAsync("1");

        // Assert
        Assert.False(response.getResponse.BaixoEstoque);
    }

    #endregion
}