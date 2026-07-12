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

public class OrdemServicoServiceTests
{
    private readonly Mock<IOrdemServicoRepository> _ordemServicoRepositoryMock;
    private readonly Mock<IVeiculoRepository> _veiculoRepositoryMock;
    private readonly Mock<INotificadorService> _notificadorMock;
    private readonly Mock<ILogger<OrdemServicoService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IOrdemServicoAprovacaoTokenRepository> _ordemServicoAprovacaoTokenRepositoryMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly OrdemServicoService _sut;

    public OrdemServicoServiceTests()
    {
        _ordemServicoRepositoryMock = new Mock<IOrdemServicoRepository>();
        _veiculoRepositoryMock      = new Mock<IVeiculoRepository>();
        _notificadorMock            = new Mock<INotificadorService>();
        _loggerMock                 = new Mock<ILogger<OrdemServicoService>>();
        _configurationMock          = new Mock<IConfiguration>();
        _ordemServicoAprovacaoTokenRepositoryMock = new Mock<IOrdemServicoAprovacaoTokenRepository>();
        _emailServiceMock           = new Mock<IEmailService>();

        _sut = new OrdemServicoService(
            _loggerMock.Object,
            _configurationMock.Object,
            _notificadorMock.Object,
            _ordemServicoRepositoryMock.Object,
            _veiculoRepositoryMock.Object,
            _ordemServicoAprovacaoTokenRepositoryMock.Object,
            _emailServiceMock.Object);
    }

    // ─── helpers ────────────────────────────────────────────────────────────────

    private static Veiculo CriarVeiculo(int id = 1, int clienteId = 1) =>
        new() { Id = id, Placa = "ABC1234", Marca = "Toyota", Modelo = "Corolla", Ano = 2022, ClienteId = clienteId, Ativo = true, DataCriacao = DateTime.Now };

    private static OrdemServico CriarOrdemServico(int id = 1, int veiculoId = 1, int clienteId = 1,
        StatusOrdemServico status = StatusOrdemServico.Recebida) =>
        new()
        {
            Id                 = id,
            DescricaoProblema  = "Barulho no motor",
            ValorTotalEstimado = 500m,
            VeiculoId          = veiculoId,
            ClienteId          = clienteId,
            Status             = status,
            DataCriacao        = DateTime.Now
        };

    // ─── AdicionarAsync ─────────────────────────────────────────────────────────

    #region AdicionarAsync

    [Fact]
    public async Task AdicionarAsync_QuandoVeiculoExisteEClienteCorreto_DeveRetornarOSComStatusRecebida()
    {
        // Arrange
        var veiculo = CriarVeiculo(clienteId: 1);
        var ordemEsperada = CriarOrdemServico();

        _veiculoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(veiculo);

        _ordemServicoRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<OrdemServico>()))
            .ReturnsAsync(ordemEsperada);

        // Act
        var resultado = await _sut.AdicionarAsync("Barulho no motor", 500m, veiculoId: 1, clienteId: 1);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(StatusOrdemServico.Recebida, resultado.Status);
        _ordemServicoRepositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<OrdemServico>()), Times.Once);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoVeiculoNaoEncontrado_DeveLancarKeyNotFoundException()
    {
        // Arrange
        _veiculoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Veiculo?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.AdicionarAsync("Descrição", 100m, veiculoId: 99, clienteId: 1));

        _ordemServicoRepositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<OrdemServico>()), Times.Never);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoVeiculoNaoPertenceAoCliente_DeveLancarInvalidOperationException()
    {
        // Arrange
        var veiculo = CriarVeiculo(clienteId: 2);

        _veiculoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(veiculo);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AdicionarAsync("Descrição", 100m, veiculoId: 1, clienteId: 1));

        _ordemServicoRepositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<OrdemServico>()), Times.Never);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoCriada_DeveDefinirStatusRecebida()
    {
        // Arrange
        var veiculo = CriarVeiculo(clienteId: 1);
        OrdemServico ordemCriada = null!;

        _veiculoRepositoryMock
            .Setup(r => r.ObterPorIdAsync("1"))
            .ReturnsAsync(veiculo);

        _ordemServicoRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<OrdemServico>()))
            .Callback<OrdemServico>(os => ordemCriada = os)
            .ReturnsAsync((OrdemServico os) => os);

        // Act
        await _sut.AdicionarAsync("Pneu furado", null, veiculoId: 1, clienteId: 1);

        // Assert
        Assert.NotNull(ordemCriada);
        Assert.Equal(StatusOrdemServico.Recebida, ordemCriada.Status);
        Assert.Equal(1, ordemCriada.ClienteId);
        Assert.Equal(1, ordemCriada.VeiculoId);
    }

    #endregion

    // ─── AtualizarAsync ─────────────────────────────────────────────────────────

    #region AtualizarAsync

    [Fact]
    public async Task AtualizarAsync_QuandoOSRecebidaEDadosCompletos_DeveAvancarParaEmDiagnostico()
    {
        // Arrange
        var existente = CriarOrdemServico(status: StatusOrdemServico.Recebida);
        var osAtualizada = new OrdemServico
        {
            Id                 = 1,
            DescricaoProblema  = "Barulho no motor revisado",
            ValorTotalEstimado = 750m,
            VeiculoId          = 1,
            ClienteId          = 1
        };

        var veiculo = CriarVeiculo(clienteId: 1);

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(existente);
        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(veiculo);
        _ordemServicoRepositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>())).ReturnsAsync(osAtualizada);

        // Act
        var resultado = await _sut.AtualizarAsync(osAtualizada);

        // Assert
        Assert.Equal(StatusOrdemServico.EmDiagnostico, osAtualizada.Status);
        Assert.NotNull(osAtualizada.DataModificacao);
        _ordemServicoRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemServico>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoOSRecebidaSemValorEstimado_NaoDeveAvancarStatus()
    {
        // Arrange
        var existente = CriarOrdemServico(status: StatusOrdemServico.Recebida);
        var osAtualizada = new OrdemServico
        {
            Id                 = 1,
            DescricaoProblema  = "Descrição preenchida",
            ValorTotalEstimado = null,
            VeiculoId          = 1,
            ClienteId          = 1
        };

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(existente);
        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(CriarVeiculo(clienteId: 1));
        _ordemServicoRepositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>())).ReturnsAsync(osAtualizada);

        // Act
        await _sut.AtualizarAsync(osAtualizada);

        // Assert
        Assert.Equal(StatusOrdemServico.Recebida, osAtualizada.Status);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoOSNaoEncontrada_DeveLancarKeyNotFoundException()
    {
        // Arrange
        var os = CriarOrdemServico();

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(os.Id.ToString()))
            .ReturnsAsync((OrdemServico?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.AtualizarAsync(os));

        _ordemServicoRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemServico>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoVeiculoNaoEncontrado_DeveLancarKeyNotFoundException()
    {
        // Arrange
        var existente    = CriarOrdemServico();
        var osAtualizada = CriarOrdemServico(veiculoId: 99);

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(existente);
        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync("99")).ReturnsAsync((Veiculo?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.AtualizarAsync(osAtualizada));

        _ordemServicoRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemServico>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoVeiculoNaoPertenceAoCliente_DeveLancarInvalidOperationException()
    {
        // Arrange
        var existente    = CriarOrdemServico(clienteId: 1);
        var osAtualizada = CriarOrdemServico(clienteId: 99);
        var veiculo      = CriarVeiculo(clienteId: 1);

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(existente);
        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(veiculo);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AtualizarAsync(osAtualizada));

        _ordemServicoRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemServico>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarAsync_DevePreservarDataCriacaoOriginal()
    {
        // Arrange
        var dataCriacaoOriginal = new DateTime(2026, 1, 1);
        var existente = CriarOrdemServico();
        existente.DataCriacao = dataCriacaoOriginal;

        var osAtualizada = CriarOrdemServico();
        var veiculo      = CriarVeiculo(clienteId: 1);

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(existente);
        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(veiculo);
        _ordemServicoRepositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>())).ReturnsAsync(osAtualizada);

        // Act
        await _sut.AtualizarAsync(osAtualizada);

        // Assert
        Assert.Equal(dataCriacaoOriginal, osAtualizada.DataCriacao);
    }

    #endregion

    // ─── IniciarDiagnosticoAsync ─────────────────────────────────────────────────

    #region IniciarDiagnosticoAsync

    [Fact]
    public async Task IniciarDiagnosticoAsync_QuandoOSRecebida_DeveAlterarStatusParaEmDiagnostico()
    {
        // Arrange
        var os = CriarOrdemServico(status: StatusOrdemServico.Recebida);

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(os);
        _ordemServicoRepositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>())).ReturnsAsync(os);

        // Act
        var resultado = await _sut.IniciarDiagnosticoAsync(1);

        // Assert
        Assert.Equal(StatusOrdemServico.EmDiagnostico, resultado.Status);
        Assert.NotNull(resultado.DataModificacao);
        _ordemServicoRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemServico>()), Times.Once);
    }

    [Fact]
    public async Task IniciarDiagnosticoAsync_QuandoOSNaoRecebida_DeveLancarInvalidOperationException()
    {
        // Arrange
        var os = CriarOrdemServico(status: StatusOrdemServico.EmDiagnostico);

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(os);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.IniciarDiagnosticoAsync(1));

        _ordemServicoRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemServico>()), Times.Never);
    }

    [Fact]
    public async Task IniciarDiagnosticoAsync_QuandoOSNaoEncontrada_DeveLancarKeyNotFoundException()
    {
        // Arrange
        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("99")).ReturnsAsync((OrdemServico?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.IniciarDiagnosticoAsync(99));
    }

    #endregion

    // ─── AguardarAprovacaoAsync ──────────────────────────────────────────────────

    #region AguardarAprovacaoAsync

    [Fact]
    public async Task AguardarAprovacaoAsync_QuandoOSEmDiagnostico_DeveAlterarStatusParaAguardandoAprovacao()
    {
        // Arrange
        var os = CriarOrdemServico(status: StatusOrdemServico.EmDiagnostico);

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(os);
        _ordemServicoRepositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>())).ReturnsAsync(os);

        // Act
        var resultado = await _sut.AguardarAprovacaoAsync(1);

        // Assert
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, resultado.Status);
        Assert.NotNull(resultado.DataModificacao);
    }

    [Fact]
    public async Task AguardarAprovacaoAsync_QuandoOSNaoEmDiagnostico_DeveLancarInvalidOperationException()
    {
        // Arrange
        var os = CriarOrdemServico(status: StatusOrdemServico.Recebida);

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(os);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AguardarAprovacaoAsync(1));
    }

    #endregion

    // ─── IniciarExecucaoAsync ────────────────────────────────────────────────────

    #region IniciarExecucaoAsync

    [Fact]
    public async Task IniciarExecucaoAsync_QuandoOSAguardandoAprovacao_DeveAlterarStatusParaEmExecucao()
    {
        // Arrange
        var os = CriarOrdemServico(status: StatusOrdemServico.AguardandoAprovacao);

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(os);
        _ordemServicoRepositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>())).ReturnsAsync(os);

        // Act
        var resultado = await _sut.IniciarExecucaoAsync(1);

        // Assert
        Assert.Equal(StatusOrdemServico.EmExecucao, resultado.Status);
        Assert.NotNull(resultado.DataModificacao);
    }

    [Fact]
    public async Task IniciarExecucaoAsync_QuandoOSNaoAguardandoAprovacao_DeveLancarInvalidOperationException()
    {
        // Arrange
        var os = CriarOrdemServico(status: StatusOrdemServico.EmDiagnostico);

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(os);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.IniciarExecucaoAsync(1));
    }

    #endregion

    // ─── FinalizarAsync ──────────────────────────────────────────────────────────

    #region FinalizarAsync

    [Fact]
    public async Task FinalizarAsync_QuandoOSEmExecucao_DeveAlterarStatusParaFinalizada()
    {
        // Arrange
        var os = CriarOrdemServico(status: StatusOrdemServico.EmExecucao);

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(os);
        _ordemServicoRepositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>())).ReturnsAsync(os);

        // Act
        var resultado = await _sut.FinalizarAsync(1);

        // Assert
        Assert.Equal(StatusOrdemServico.Finalizada, resultado.Status);
        Assert.NotNull(resultado.DataModificacao);
    }

    [Fact]
    public async Task FinalizarAsync_QuandoOSNaoEmExecucao_DeveLancarInvalidOperationException()
    {
        // Arrange
        var os = CriarOrdemServico(status: StatusOrdemServico.AguardandoAprovacao);

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(os);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.FinalizarAsync(1));
    }

    #endregion

    // ─── EntregarAsync ───────────────────────────────────────────────────────────

    #region EntregarAsync

    [Fact]
    public async Task EntregarAsync_QuandoOSFinalizada_DeveAlterarStatusParaEntregue()
    {
        // Arrange
        var os = CriarOrdemServico(status: StatusOrdemServico.Finalizada);

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(os);
        _ordemServicoRepositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>())).ReturnsAsync(os);

        // Act
        var resultado = await _sut.EntregarAsync(1);

        // Assert
        Assert.Equal(StatusOrdemServico.Entregue, resultado.Status);
        Assert.NotNull(resultado.DataModificacao);
    }

    [Fact]
    public async Task EntregarAsync_QuandoOSNaoFinalizada_DeveLancarInvalidOperationException()
    {
        // Arrange
        var os = CriarOrdemServico(status: StatusOrdemServico.EmExecucao);

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(os);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.EntregarAsync(1));
    }

    #endregion

    // ─── ObterTodosAsync ────────────────────────────────────────────────────────

    #region ObterTodosAsync

    [Fact]
    public async Task ObterTodosAsync_DeveRetornarListaDeOrdens()
    {
        // Arrange
        var lista = new List<OrdemServico>
        {
            CriarOrdemServico(id: 1),
            CriarOrdemServico(id: 2)
        };

        _ordemServicoRepositoryMock.Setup(r => r.ObterTodosAsync()).ReturnsAsync(lista);

        // Act
        var resultado = await _sut.ObterTodosAsync();

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(2, resultado.Count());
    }

    [Fact]
    public async Task ObterTodosAsync_QuandoRepositorioLancaExcecao_DeveRePropagar()
    {
        // Arrange
        _ordemServicoRepositoryMock
            .Setup(r => r.ObterTodosAsync())
            .ThrowsAsync(new Exception("Erro no banco"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.ObterTodosAsync());
    }

    #endregion

    // ─── ObterPorClienteIdAsync ─────────────────────────────────────────────────

    #region ObterPorClienteIdAsync

    [Fact]
    public async Task ObterPorClienteIdAsync_DeveRetornarOrdensDoCliente()
    {
        // Arrange
        var clienteId = 1;
        var lista = new List<OrdemServico>
        {
            CriarOrdemServico(id: 1, clienteId: clienteId),
            CriarOrdemServico(id: 2, clienteId: clienteId)
        };

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterPorClienteIdAsync(clienteId))
            .ReturnsAsync(lista);

        // Act
        var resultado = await _sut.ObterPorClienteIdAsync(clienteId.ToString());

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(2, resultado.Count());
        Assert.All(resultado, os => Assert.Equal(clienteId, os.ClienteId));
    }

    [Fact]
    public async Task ObterPorClienteIdAsync_QuandoClienteIdInvalido_DeveLancarArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.ObterPorClienteIdAsync("nao-e-um-numero"));

        _ordemServicoRepositoryMock.Verify(r => r.ObterPorClienteIdAsync(It.IsAny<int>()), Times.Never);
    }

    #endregion

    // ─── ObterPorIdAsync ────────────────────────────────────────────────────────

    #region ObterPorIdAsync

    [Fact]
    public async Task ObterPorIdAsync_QuandoEncontrada_DeveRetornarOS()
    {
        // Arrange
        var os = CriarOrdemServico();

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(os);

        // Act
        var resultado = await _sut.ObterPorIdAsync("1");

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(os.Id, resultado.Id);
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoNaoEncontrada_DeveRetornarNull()
    {
        // Arrange
        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("999")).ReturnsAsync((OrdemServico?)null);

        // Act
        var resultado = await _sut.ObterPorIdAsync("999");

        // Assert
        Assert.Null(resultado);
    }

    #endregion

    // ─── RemoverAsync ───────────────────────────────────────────────────────────

    #region RemoverAsync

    [Fact]
    public async Task RemoverAsync_QuandoOSExiste_DeveChamarRepositorio()
    {
        // Arrange
        var id = "1";
        var os = CriarOrdemServico();

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(os);
        _ordemServicoRepositoryMock.Setup(r => r.RemoverAsync(id)).Returns(Task.CompletedTask);

        // Act
        await _sut.RemoverAsync(id);

        // Assert
        _ordemServicoRepositoryMock.Verify(r => r.RemoverAsync(id), Times.Once);
    }

    [Fact]
    public async Task RemoverAsync_QuandoOSNaoExiste_DeveLancarKeyNotFoundException()
    {
        // Arrange
        var id = "999";

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync((OrdemServico?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.RemoverAsync(id));

        _ordemServicoRepositoryMock.Verify(r => r.RemoverAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion
}