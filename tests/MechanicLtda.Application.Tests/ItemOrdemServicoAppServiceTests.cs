using AutoMapper;
using MechanicLtda.Application.AppServices;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Services;
using Moq;
using Xunit;

namespace MechanicLtda.Application.Tests;

public class ItemOrdemServicoAppServiceTests
{
    private readonly Mock<IItemOrdemServicoService> _serviceMock;
    private readonly Mock<IMapper>                  _mapperMock;
    private readonly ItemOrdemServicoAppService     _sut;

    public ItemOrdemServicoAppServiceTests()
    {
        _serviceMock = new Mock<IItemOrdemServicoService>();
        _mapperMock  = new Mock<IMapper>();
        _sut = new ItemOrdemServicoAppService(_serviceMock.Object, _mapperMock.Object);
    }

    // ─── helpers ────────────────────────────────────────────────────────────────

    private static ItemOrdemServico CriarEntidade(int id = 1, int ordemServicoId = 1,
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

    private static ItemOrdemServicoDto CriarDto(int id = 1, int ordemServicoId = 1,
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

    // ─── AdicionarAsync ─────────────────────────────────────────────────────────

    #region AdicionarAsync

    [Fact]
    public async Task AdicionarAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var createDto  = new ItemOrdemServicoCreateDto { Quantidade = 2, ValorUnitario = 100m };
        var entidade   = CriarEntidade();
        var itemDto    = CriarDto();

        _serviceMock
            .Setup(s => s.AdicionarAsync(1, createDto.EstoqueId, createDto.Quantidade, createDto.ValorUnitario))
            .ReturnsAsync(entidade);

        _mapperMock
            .Setup(m => m.Map<ItemOrdemServicoDto>(entidade))
            .Returns(itemDto);

        // Act
        var response = await _sut.AdicionarAsync(1, createDto);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(itemDto, response.getResponse);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoOSNaoEncontrada_DeveRetornarResponseComErro()
    {
        // Arrange
        var createDto = new ItemOrdemServicoCreateDto { Quantidade = 1, ValorUnitario = 50m };

        _serviceMock
            .Setup(s => s.AdicionarAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<decimal>()))
            .ThrowsAsync(new KeyNotFoundException("Ordem de Serviço com Id '99' não encontrada."));

        // Act
        var response = await _sut.AdicionarAsync(99, createDto);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoServicoLancaExcecaoGenerica_DeveRetornarResponseComErro()
    {
        // Arrange
        var createDto = new ItemOrdemServicoCreateDto { Quantidade = 1, ValorUnitario = 50m };

        _serviceMock
            .Setup(s => s.AdicionarAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<decimal>()))
            .ThrowsAsync(new Exception("Erro inesperado."));

        // Act
        var response = await _sut.AdicionarAsync(1, createDto);

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
        var id        = "1";
        var updateDto = new ItemOrdemServicoUpdateDto { Quantidade = 5, ValorUnitario = 80m };
        var entidade  = CriarEntidade(quantidade: 5, valorUnitario: 80m);
        var itemDto   = CriarDto(quantidade: 5, valorUnitario: 80m);

        _mapperMock
            .Setup(m => m.Map<ItemOrdemServico>(updateDto))
            .Returns(entidade);

        _serviceMock
            .Setup(s => s.AtualizarAsync(It.IsAny<ItemOrdemServico>()))
            .ReturnsAsync(entidade);

        _mapperMock
            .Setup(m => m.Map<ItemOrdemServicoDto>(entidade))
            .Returns(itemDto);

        // Act
        var response = await _sut.AtualizarAsync(id, updateDto);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(itemDto, response.getResponse);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoItemNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        var updateDto = new ItemOrdemServicoUpdateDto { Quantidade = 1, ValorUnitario = 10m };

        _mapperMock
            .Setup(m => m.Map<ItemOrdemServico>(updateDto))
            .Returns(new ItemOrdemServico());

        _serviceMock
            .Setup(s => s.AtualizarAsync(It.IsAny<ItemOrdemServico>()))
            .ThrowsAsync(new KeyNotFoundException("Item com Id '999' não encontrado."));

        // Act
        var response = await _sut.AtualizarAsync("999", updateDto);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoServicoLancaExcecaoGenerica_DeveRetornarResponseComErro()
    {
        // Arrange
        var updateDto = new ItemOrdemServicoUpdateDto { Quantidade = 1, ValorUnitario = 10m };

        _mapperMock
            .Setup(m => m.Map<ItemOrdemServico>(updateDto))
            .Returns(new ItemOrdemServico());

        _serviceMock
            .Setup(s => s.AtualizarAsync(It.IsAny<ItemOrdemServico>()))
            .ThrowsAsync(new Exception("Erro inesperado."));

        // Act
        var response = await _sut.AtualizarAsync("1", updateDto);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
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
    public async Task RemoverAsync_QuandoItemNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        var id = "999";

        _serviceMock
            .Setup(s => s.RemoverAsync(id))
            .ThrowsAsync(new KeyNotFoundException($"Item com Id '{id}' não encontrado."));

        // Act
        var response = await _sut.RemoverAsync(id);

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

    // ─── ObterPorOrdemServicoIdAsync ────────────────────────────────────────────

    #region ObterPorOrdemServicoIdAsync

    [Fact]
    public async Task ObterPorOrdemServicoIdAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var ordemServicoId = 1;
        var entidades = new List<ItemOrdemServico> { CriarEntidade(1), CriarEntidade(2) };
        var dtos      = new List<ItemOrdemServicoDto> { CriarDto(1), CriarDto(2) };

        _serviceMock.Setup(s => s.ObterPorOrdemServicoIdAsync(ordemServicoId)).ReturnsAsync(entidades);
        _mapperMock.Setup(m => m.Map<IEnumerable<ItemOrdemServicoDto>>(entidades)).Returns(dtos);

        // Act
        var response = await _sut.ObterPorOrdemServicoIdAsync(ordemServicoId);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(2, response.getResponse.Count());
    }

    [Fact]
    public async Task ObterPorOrdemServicoIdAsync_QuandoServicoLancaExcecao_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.ObterPorOrdemServicoIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Erro no banco"));

        // Act
        var response = await _sut.ObterPorOrdemServicoIdAsync(1);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    // ─── ObterPorIdAsync ────────────────────────────────────────────────────────

    #region ObterPorIdAsync

    [Fact]
    public async Task ObterPorIdAsync_QuandoItemEncontrado_DeveRetornarResponseSemErros()
    {
        // Arrange
        var id      = "1";
        var entidade = CriarEntidade();
        var itemDto  = CriarDto();

        _serviceMock.Setup(s => s.ObterPorIdAsync(id)).ReturnsAsync(entidade);
        _mapperMock.Setup(m => m.Map<ItemOrdemServicoDto>(entidade)).Returns(itemDto);

        // Act
        var response = await _sut.ObterPorIdAsync(id);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(itemDto, response.getResponse);
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoItemNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        var id = "999";

        _serviceMock.Setup(s => s.ObterPorIdAsync(id)).ReturnsAsync((ItemOrdemServico?)null);

        // Act
        var response = await _sut.ObterPorIdAsync(id);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion
}