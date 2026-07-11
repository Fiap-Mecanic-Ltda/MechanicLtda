using AutoMapper;
using MechanicLtda.Application.AppServices;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Services;
using Moq;
using Xunit;

namespace MechanicLtda.Application.Tests;

public class ServicoOficinaAppServiceTests
{
    private readonly Mock<IServicoOficinaService> _serviceMock;
    private readonly Mock<IMapper>                _mapperMock;
    private readonly ServicoOficinaAppService     _sut;

    public ServicoOficinaAppServiceTests()
    {
        _serviceMock = new Mock<IServicoOficinaService>();
        _mapperMock  = new Mock<IMapper>();
        _sut = new ServicoOficinaAppService(_serviceMock.Object, _mapperMock.Object);
    }

    // ─── helpers ────────────────────────────────────────────────────────────────

    private static ServicoOficina CriarServico(
        int id            = 1,
        string nome       = "Troca de Óleo",
        decimal valorBase = 150m,
        bool ativo        = true) =>
        new()
        {
            Id           = id,
            Nome         = nome,
            Descricao    = "Troca de óleo e filtro",
            ValorBase    = valorBase,
            Ativo        = ativo,
            DataCadastro = DateTime.UtcNow
        };

    private static ServicoOficinaDto CriarDto(
        int id            = 1,
        string nome       = "Troca de Óleo",
        decimal valorBase = 150m,
        bool ativo        = true) =>
        new()
        {
            Id           = id,
            Nome         = nome,
            Descricao    = "Troca de óleo e filtro",
            ValorBase    = valorBase,
            Ativo        = ativo,
            DataCadastro = DateTime.UtcNow
        };

    // ─── AdicionarAsync ─────────────────────────────────────────────────────────

    #region AdicionarAsync

    [Fact]
    public async Task AdicionarAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var createDto = new ServicoOficinaCreateDto { Nome = "Troca de Óleo", Descricao = "Troca de óleo e filtro", ValorBase = 150m };
        var entidade  = CriarServico();
        var dto       = CriarDto();

        _mapperMock.Setup(m => m.Map<ServicoOficina>(createDto)).Returns(entidade);
        _mapperMock.Setup(m => m.Map<ServicoOficinaDto>(entidade)).Returns(dto);
        _serviceMock.Setup(s => s.AdicionarAsync(entidade)).ReturnsAsync(entidade);

        // Act
        var response = await _sut.AdicionarAsync(createDto);

        // Assert
        Assert.False(response.hasErrors);
        Assert.NotNull(response.getResponse);
        Assert.Equal("Troca de Óleo", response.getResponse.Nome);
        _serviceMock.Verify(s => s.AdicionarAsync(entidade), Times.Once);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoServicoLancaExcecao_DeveRetornarResponseComErro()
    {
        // Arrange
        var createDto = new ServicoOficinaCreateDto { Nome = "X", Descricao = "Y", ValorBase = 10m };
        var entidade  = CriarServico();

        _mapperMock.Setup(m => m.Map<ServicoOficina>(createDto)).Returns(entidade);
        _serviceMock
            .Setup(s => s.AdicionarAsync(It.IsAny<ServicoOficina>()))
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
        var updateDto = new ServicoOficinaUpdateDto { Nome = "Troca de Óleo Sintético", Descricao = "Y", ValorBase = 220m, Ativo = true };
        var entidade  = CriarServico(nome: "Troca de Óleo Sintético", valorBase: 220m);
        var dto       = CriarDto(nome: "Troca de Óleo Sintético", valorBase: 220m);

        _mapperMock.Setup(m => m.Map<ServicoOficina>(updateDto)).Returns(entidade);
        _mapperMock.Setup(m => m.Map<ServicoOficinaDto>(entidade)).Returns(dto);
        _serviceMock.Setup(s => s.AtualizarAsync(It.IsAny<ServicoOficina>())).ReturnsAsync(entidade);

        // Act
        var response = await _sut.AtualizarAsync("1", updateDto);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal("Troca de Óleo Sintético", response.getResponse.Nome);
        _serviceMock.Verify(s => s.AtualizarAsync(It.IsAny<ServicoOficina>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoServicoNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        var updateDto = new ServicoOficinaUpdateDto { Nome = "X", Descricao = "Y", ValorBase = 10m };

        _mapperMock.Setup(m => m.Map<ServicoOficina>(updateDto)).Returns(new ServicoOficina());
        _serviceMock
            .Setup(s => s.AtualizarAsync(It.IsAny<ServicoOficina>()))
            .ThrowsAsync(new KeyNotFoundException("Serviço com Id '99' não encontrado."));

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
        var updateDto = new ServicoOficinaUpdateDto { Nome = "X", Descricao = "Y", ValorBase = 10m };
        ServicoOficina entidadeSalva = null!;

        _mapperMock.Setup(m => m.Map<ServicoOficina>(updateDto)).Returns(new ServicoOficina());
        _serviceMock
            .Setup(s => s.AtualizarAsync(It.IsAny<ServicoOficina>()))
            .Callback<ServicoOficina>(e => entidadeSalva = e)
            .ReturnsAsync(CriarServico(id: 5));

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
    public async Task RemoverAsync_QuandoServicoNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.RemoverAsync("99"))
            .ThrowsAsync(new KeyNotFoundException("Serviço com Id '99' não encontrado."));

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
        var lista = new List<ServicoOficina> { CriarServico(id: 1), CriarServico(id: 2) };
        var dtos  = new List<ServicoOficinaDto> { CriarDto(id: 1), CriarDto(id: 2) };

        _serviceMock.Setup(s => s.ObterTodosAsync()).ReturnsAsync(lista);
        _mapperMock.Setup(m => m.Map<IEnumerable<ServicoOficinaDto>>(lista)).Returns(dtos);

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
        _serviceMock.Setup(s => s.ObterTodosAsync()).ThrowsAsync(new Exception("Erro no banco"));

        // Act
        var response = await _sut.ObterTodosAsync();

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    // ─── ObterAtivosAsync ───────────────────────────────────────────────────────

    #region ObterAtivosAsync

    [Fact]
    public async Task ObterAtivosAsync_QuandoSucesso_DeveRetornarApenasAtivos()
    {
        // Arrange
        var lista = new List<ServicoOficina> { CriarServico(id: 1, ativo: true) };
        var dtos  = new List<ServicoOficinaDto> { CriarDto(id: 1, ativo: true) };

        _serviceMock.Setup(s => s.ObterAtivosAsync()).ReturnsAsync(lista);
        _mapperMock.Setup(m => m.Map<IEnumerable<ServicoOficinaDto>>(lista)).Returns(dtos);

        // Act
        var response = await _sut.ObterAtivosAsync();

        // Assert
        Assert.False(response.hasErrors);
        Assert.Single(response.getResponse);
        Assert.All(response.getResponse, d => Assert.True(d.Ativo));
    }

    [Fact]
    public async Task ObterAtivosAsync_QuandoServicoLancaExcecao_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock.Setup(s => s.ObterAtivosAsync()).ThrowsAsync(new Exception("Erro no banco"));

        // Act
        var response = await _sut.ObterAtivosAsync();

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
        var servico = CriarServico();
        var dto     = CriarDto();

        _serviceMock.Setup(s => s.ObterPorIdAsync("1")).ReturnsAsync(servico);
        _mapperMock.Setup(m => m.Map<ServicoOficinaDto>(servico)).Returns(dto);

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
        _serviceMock.Setup(s => s.ObterPorIdAsync("999")).ReturnsAsync((ServicoOficina?)null);

        // Act
        var response = await _sut.ObterPorIdAsync("999");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion
}
