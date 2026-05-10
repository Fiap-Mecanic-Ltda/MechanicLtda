using AutoMapper;
using MechanicLtda.Application.AppServices;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Services;
using Moq;
using Xunit;

namespace MechanicLtda.Application.Tests;

public class OrcamentoAppServiceTests
{
    private readonly Mock<IOrcamentoService>    _serviceMock;
    private readonly Mock<IOrcamentoPdfAppService> _pdfService;
    private readonly Mock<IMapper>              _mapperMock;
    private readonly OrcamentoAppService        _sut;

    public OrcamentoAppServiceTests()
    {
        _serviceMock = new Mock<IOrcamentoService>();
        _pdfService  = new Mock<IOrcamentoPdfAppService>();
        _mapperMock  = new Mock<IMapper>();
        _sut = new OrcamentoAppService(_serviceMock.Object, _pdfService.Object, _mapperMock.Object);
    }

    // ─── helpers ────────────────────────────────────────────────────────────────

    private static Orcamento CriarEntidade(
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

    private static OrcamentoDto CriarDto(
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

    // ─── ObterPorIdAsync ────────────────────────────────────────────────────────

    #region ObterPorIdAsync

    [Fact]
    public async Task ObterPorIdAsync_QuandoEncontrado_DeveRetornarResponseSemErros()
    {
        // Arrange
        var entidade = CriarEntidade();
        var dto      = CriarDto();

        _serviceMock.Setup(s => s.ObterPorIdAsync("1")).ReturnsAsync(entidade);
        _mapperMock.Setup(m => m.Map<OrcamentoDto>(entidade)).Returns(dto);

        // Act
        var response = await _sut.ObterPorIdAsync("1");

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(dto, response.getResponse);
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.ObterPorIdAsync("999"))
            .ReturnsAsync((Orcamento?)null);

        // Act
        var response = await _sut.ObterPorIdAsync("999");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoServicoLancaExcecao_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.ObterPorIdAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Erro no banco"));

        // Act
        var response = await _sut.ObterPorIdAsync("1");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    // ─── ObterPorOrdemServicoIdAsync ────────────────────────────────────────────

    #region ObterPorOrdemServicoIdAsync

    [Fact]
    public async Task ObterPorOrdemServicoIdAsync_QuandoEncontrado_DeveRetornarResponseSemErros()
    {
        // Arrange
        var entidade = CriarEntidade(ordemServicoId: 1);
        var dto      = CriarDto(ordemServicoId: 1);

        _serviceMock.Setup(s => s.ObterPorOrdemServicoIdAsync(1)).ReturnsAsync(entidade);
        _mapperMock.Setup(m => m.Map<OrcamentoDto>(entidade)).Returns(dto);

        // Act
        var response = await _sut.ObterPorOrdemServicoIdAsync(1);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(1, response.getResponse.OrdemServicoId);
    }

    [Fact]
    public async Task ObterPorOrdemServicoIdAsync_QuandoNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.ObterPorOrdemServicoIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Orcamento?)null);

        // Act
        var response = await _sut.ObterPorOrdemServicoIdAsync(99);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
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

    // ─── AtualizarManualAsync ───────────────────────────────────────────────────

    #region AtualizarManualAsync

    [Fact]
    public async Task AtualizarManualAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var updateDto = new OrcamentoUpdateDto { ValorTotalPecas = 350m, ValorTotalInsumos = 150m };
        var entidade  = CriarEntidade(pecas: 350m, insumos: 150m);
        var dto       = CriarDto(pecas: 350m, insumos: 150m);

        _mapperMock
            .Setup(m => m.Map<Orcamento>(updateDto))
            .Returns(entidade);

        _serviceMock
            .Setup(s => s.AtualizarManualAsync(It.IsAny<Orcamento>()))
            .ReturnsAsync(entidade);

        _mapperMock
            .Setup(m => m.Map<OrcamentoDto>(entidade))
            .Returns(dto);

        // Act
        var response = await _sut.AtualizarManualAsync("1", updateDto);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(dto, response.getResponse);
    }

    [Fact]
    public async Task AtualizarManualAsync_DeveDefinirIdNoOrcamentoMapeado()
    {
        // Arrange
        var updateDto = new OrcamentoUpdateDto { ValorTotalPecas = 100m, ValorTotalInsumos = 50m };
        var entidade  = new Orcamento { Id = 0 }; // Id ainda não definido pelo mapper
        var dto       = CriarDto();

        _mapperMock
            .Setup(m => m.Map<Orcamento>(updateDto))
            .Returns(entidade);

        Orcamento orcamentoPassado = null!;
        _serviceMock
            .Setup(s => s.AtualizarManualAsync(It.IsAny<Orcamento>()))
            .Callback<Orcamento>(o => orcamentoPassado = o)
            .ReturnsAsync(CriarEntidade());

        _mapperMock
            .Setup(m => m.Map<OrcamentoDto>(It.IsAny<Orcamento>()))
            .Returns(dto);

        // Act
        await _sut.AtualizarManualAsync("5", updateDto);

        // Assert — o AppService deve definir o Id a partir do parâmetro de rota
        Assert.Equal(5, orcamentoPassado.Id);
    }

    [Fact]
    public async Task AtualizarManualAsync_QuandoOrcamentoNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        var updateDto = new OrcamentoUpdateDto { ValorTotalPecas = 100m, ValorTotalInsumos = 50m };

        _mapperMock
            .Setup(m => m.Map<Orcamento>(updateDto))
            .Returns(new Orcamento());

        _serviceMock
            .Setup(s => s.AtualizarManualAsync(It.IsAny<Orcamento>()))
            .ThrowsAsync(new KeyNotFoundException("Orçamento com Id '999' não encontrado."));

        // Act
        var response = await _sut.AtualizarManualAsync("999", updateDto);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    [Fact]
    public async Task AtualizarManualAsync_QuandoServicoLancaExcecaoGenerica_DeveRetornarResponseComErro()
    {
        // Arrange
        var updateDto = new OrcamentoUpdateDto { ValorTotalPecas = 100m, ValorTotalInsumos = 50m };

        _mapperMock
            .Setup(m => m.Map<Orcamento>(updateDto))
            .Returns(new Orcamento());

        _serviceMock
            .Setup(s => s.AtualizarManualAsync(It.IsAny<Orcamento>()))
            .ThrowsAsync(new Exception("Erro inesperado."));

        // Act
        var response = await _sut.AtualizarManualAsync("1", updateDto);

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
    public async Task RemoverAsync_QuandoOrcamentoNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        var id = "999";

        _serviceMock
            .Setup(s => s.RemoverAsync(id))
            .ThrowsAsync(new KeyNotFoundException($"Orçamento com Id '{id}' não encontrado."));

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
}