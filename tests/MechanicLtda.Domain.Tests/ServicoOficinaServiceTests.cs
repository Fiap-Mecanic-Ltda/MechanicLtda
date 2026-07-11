using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Domain.Interfaces.Services;
using MechanicLtda.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MechanicLtda.Domain.Tests;

public class ServicoOficinaServiceTests
{
    private readonly Mock<IServicoOficinaRepository>      _repositoryMock;
    private readonly Mock<INotificadorService>             _notificadorMock;
    private readonly Mock<ILogger<ServicoOficinaService>>  _loggerMock;
    private readonly Mock<IConfiguration>                  _configurationMock;
    private readonly ServicoOficinaService                 _sut;

    public ServicoOficinaServiceTests()
    {
        _repositoryMock     = new Mock<IServicoOficinaRepository>();
        _notificadorMock    = new Mock<INotificadorService>();
        _loggerMock         = new Mock<ILogger<ServicoOficinaService>>();
        _configurationMock  = new Mock<IConfiguration>();

        _sut = new ServicoOficinaService(
            _loggerMock.Object,
            _configurationMock.Object,
            _notificadorMock.Object,
            _repositoryMock.Object);
    }

    // ─── helpers ────────────────────────────────────────────────────────────────

    private static ServicoOficina CriarServico(
        int id             = 1,
        string nome        = "Troca de Óleo",
        string descricao   = "Troca de óleo e filtro",
        decimal valorBase  = 150m,
        bool ativo         = true) =>
        new()
        {
            Id           = id,
            Nome         = nome,
            Descricao    = descricao,
            ValorBase    = valorBase,
            Ativo        = ativo,
            DataCadastro = DateTime.UtcNow
        };

    // ─── AdicionarAsync ─────────────────────────────────────────────────────────

    #region AdicionarAsync

    [Fact]
    public async Task AdicionarAsync_QuandoValido_DeveRetornarServicoCriado()
    {
        // Arrange
        var servico = CriarServico();

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<ServicoOficina>()))
            .ReturnsAsync(servico);

        // Act
        var resultado = await _sut.AdicionarAsync(servico);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal("Troca de Óleo", resultado.Nome);
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<ServicoOficina>()), Times.Once);
    }

    [Fact]
    public async Task AdicionarAsync_DeveDefinirDataCadastroEDataAtualizacaoNula()
    {
        // Arrange
        ServicoOficina servicoSalvo = null!;
        var servico = CriarServico();
        servico.DataAtualizacao = DateTime.UtcNow; // valor indevido que deve ser zerado

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<ServicoOficina>()))
            .Callback<ServicoOficina>(s => servicoSalvo = s)
            .ReturnsAsync((ServicoOficina s) => s);

        // Act
        await _sut.AdicionarAsync(servico);

        // Assert
        Assert.NotNull(servicoSalvo);
        Assert.Null(servicoSalvo.DataAtualizacao);
        Assert.True(servicoSalvo.DataCadastro > DateTime.MinValue);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoRepositorioLancaExcecao_DeveRePropagar()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<ServicoOficina>()))
            .ThrowsAsync(new Exception("Erro no banco"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.AdicionarAsync(CriarServico()));
    }

    #endregion

    // ─── AtualizarAsync ─────────────────────────────────────────────────────────

    #region AtualizarAsync

    [Fact]
    public async Task AtualizarAsync_QuandoServicoExiste_DeveRetornarServicoAtualizado()
    {
        // Arrange
        var existente   = CriarServico();
        var atualizacao = CriarServico(nome: "Troca de Óleo Sintético", valorBase: 220m, ativo: false);

        _repositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(existente);
        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<ServicoOficina>()))
            .ReturnsAsync((ServicoOficina s) => s);

        // Act
        var resultado = await _sut.AtualizarAsync(atualizacao);

        // Assert
        Assert.Equal("Troca de Óleo Sintético", resultado.Nome);
        Assert.Equal(220m, resultado.ValorBase);
        Assert.False(resultado.Ativo);
        Assert.NotNull(resultado.DataAtualizacao);
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<ServicoOficina>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoServicoNaoEncontrado_DeveLancarKeyNotFoundException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ServicoOficina?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.AtualizarAsync(CriarServico(id: 99)));

        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<ServicoOficina>()), Times.Never);
    }

    #endregion

    // ─── RemoverAsync ───────────────────────────────────────────────────────────

    #region RemoverAsync

    [Fact]
    public async Task RemoverAsync_QuandoServicoExiste_DeveChamarRepositorio()
    {
        // Arrange
        var id      = "1";
        var servico = CriarServico();

        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(servico);
        _repositoryMock.Setup(r => r.RemoverAsync(id)).Returns(Task.CompletedTask);

        // Act
        await _sut.RemoverAsync(id);

        // Assert
        _repositoryMock.Verify(r => r.RemoverAsync(id), Times.Once);
    }

    [Fact]
    public async Task RemoverAsync_QuandoServicoNaoEncontrado_DeveLancarKeyNotFoundException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ObterPorIdAsync("999"))
            .ReturnsAsync((ServicoOficina?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.RemoverAsync("999"));

        _repositoryMock.Verify(r => r.RemoverAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion

    // ─── ObterTodosAsync ────────────────────────────────────────────────────────

    #region ObterTodosAsync

    [Fact]
    public async Task ObterTodosAsync_DeveRetornarTodosOsServicos()
    {
        // Arrange
        var lista = new List<ServicoOficina> { CriarServico(id: 1), CriarServico(id: 2, ativo: false) };

        _repositoryMock.Setup(r => r.ObterTodosAsync()).ReturnsAsync(lista);

        // Act
        var resultado = await _sut.ObterTodosAsync();

        // Assert
        Assert.Equal(2, resultado.Count());
    }

    [Fact]
    public async Task ObterTodosAsync_QuandoRepositorioLancaExcecao_DeveRePropagar()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ObterTodosAsync()).ThrowsAsync(new Exception("Erro no banco"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.ObterTodosAsync());
    }

    #endregion

    // ─── ObterAtivosAsync ───────────────────────────────────────────────────────

    #region ObterAtivosAsync

    [Fact]
    public async Task ObterAtivosAsync_DeveRetornarApenasServicosAtivos()
    {
        // Arrange
        var lista = new List<ServicoOficina> { CriarServico(id: 1, ativo: true) };

        _repositoryMock.Setup(r => r.ObterAtivosAsync()).ReturnsAsync(lista);

        // Act
        var resultado = await _sut.ObterAtivosAsync();

        // Assert
        Assert.Single(resultado);
        Assert.All(resultado, s => Assert.True(s.Ativo));
    }

    [Fact]
    public async Task ObterAtivosAsync_QuandoRepositorioLancaExcecao_DeveRePropagar()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ObterAtivosAsync()).ThrowsAsync(new Exception("Erro no banco"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.ObterAtivosAsync());
    }

    #endregion

    // ─── ObterPorIdAsync ────────────────────────────────────────────────────────

    #region ObterPorIdAsync

    [Fact]
    public async Task ObterPorIdAsync_QuandoEncontrado_DeveRetornarServico()
    {
        // Arrange
        var servico = CriarServico();

        _repositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(servico);

        // Act
        var resultado = await _sut.ObterPorIdAsync("1");

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(servico.Id, resultado!.Id);
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoNaoEncontrado_DeveRetornarNull()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ObterPorIdAsync("999")).ReturnsAsync((ServicoOficina?)null);

        // Act
        var resultado = await _sut.ObterPorIdAsync("999");

        // Assert
        Assert.Null(resultado);
    }

    #endregion
}
