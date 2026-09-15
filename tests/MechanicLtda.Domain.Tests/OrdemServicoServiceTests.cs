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
    private readonly Mock<IMonitoramentoService> _monitoramentoMock;
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
        _monitoramentoMock          = new Mock<IMonitoramentoService>();

        _sut = new OrdemServicoService(
            _loggerMock.Object,
            _configurationMock.Object,
            _notificadorMock.Object,
            _ordemServicoRepositoryMock.Object,
            _veiculoRepositoryMock.Object,
            _ordemServicoAprovacaoTokenRepositoryMock.Object,
            _emailServiceMock.Object,
            _monitoramentoMock.Object);
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
    public async Task AguardarAprovacaoAsync_DeveEnviarLinksDeAprovacaoEmMinusculas()
    {
        // O roteamento do API Gateway diferencia maiúsculas: a rota pública é
        // /api/aprovacaoordemservico/{token}/... Um link em PascalCase cairia na
        // rota protegida e o cliente receberia 401 ao clicar no e-mail.

        // Arrange
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:BaseUrlAprovacao"]             = "https://gateway.exemplo.com/",
                ["AppSettings:AprovacaoTokenExpiracaoHoras"] = "72"
            })
            .Build();

        var sut = new OrdemServicoService(
            _loggerMock.Object,
            configuracao,
            _notificadorMock.Object,
            _ordemServicoRepositoryMock.Object,
            _veiculoRepositoryMock.Object,
            _ordemServicoAprovacaoTokenRepositoryMock.Object,
            _emailServiceMock.Object,
            _monitoramentoMock.Object);

        var os = CriarOrdemServico(status: StatusOrdemServico.EmDiagnostico);
        os.Cliente = new Cliente { Id = 1, Nome = "Fernanda Lima", Email = "fernanda@email.com", CpfCnpj = "52998224725", Ativo = true };

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(os);
        _ordemServicoRepositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>())).ReturnsAsync(os);

        var corpos = new List<string>();
        _emailServiceMock
            .Setup(e => e.EnviarEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string, string>((_, _, _, corpo) => corpos.Add(corpo))
            .Returns(Task.CompletedTask);

        // Act
        await sut.AguardarAprovacaoAsync(1);

        // Assert
        var corpoAprovacao = Assert.Single(corpos, c => c.Contains("/aprovar"));
        Assert.Contains("https://gateway.exemplo.com/api/aprovacaoordemservico/", corpoAprovacao);
        Assert.DoesNotContain("AprovacaoOrdemServico", corpoAprovacao);
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

    [Fact]
    public async Task ObterTodosAsync_DeveOrdenarPorPrioridadeDeStatus_ExecucaoPrimeiro()
    {
        // Arrange — ordem de inserção propositalmente embaralhada
        var recebida = CriarOrdemServico(id: 1, status: StatusOrdemServico.Recebida);
        var diagnostico = CriarOrdemServico(id: 2, status: StatusOrdemServico.EmDiagnostico);
        var aguardandoAprovacao = CriarOrdemServico(id: 3, status: StatusOrdemServico.AguardandoAprovacao);
        var emExecucao = CriarOrdemServico(id: 4, status: StatusOrdemServico.EmExecucao);

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterTodosAsync())
            .ReturnsAsync([recebida, diagnostico, aguardandoAprovacao, emExecucao]);

        // Act
        var resultado = (await _sut.ObterTodosAsync()).ToList();

        // Assert — Em Execução > Aguardando Aprovação > Diagnóstico > Recebida
        Assert.Equal(4, resultado.Count);
        Assert.Equal(StatusOrdemServico.EmExecucao, resultado[0].Status);
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, resultado[1].Status);
        Assert.Equal(StatusOrdemServico.EmDiagnostico, resultado[2].Status);
        Assert.Equal(StatusOrdemServico.Recebida, resultado[3].Status);
    }

    [Fact]
    public async Task ObterTodosAsync_DentroDoMesmoStatus_DeveOrdenarMaisAntigasPrimeiro()
    {
        // Arrange
        var maisRecente = CriarOrdemServico(id: 1, status: StatusOrdemServico.Recebida);
        maisRecente.DataCriacao = new DateTime(2026, 6, 1);

        var maisAntiga = CriarOrdemServico(id: 2, status: StatusOrdemServico.Recebida);
        maisAntiga.DataCriacao = new DateTime(2026, 1, 1);

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterTodosAsync())
            .ReturnsAsync([maisRecente, maisAntiga]);

        // Act
        var resultado = (await _sut.ObterTodosAsync()).ToList();

        // Assert
        Assert.Equal(2, resultado.Count);
        Assert.Equal(maisAntiga.Id, resultado[0].Id);
        Assert.Equal(maisRecente.Id, resultado[1].Id);
    }

    [Fact]
    public async Task ObterTodosAsync_DeveExcluirLogicamenteFinalizadasEEntregues()
    {
        // Arrange
        var recebida = CriarOrdemServico(id: 1, status: StatusOrdemServico.Recebida);
        var finalizada = CriarOrdemServico(id: 2, status: StatusOrdemServico.Finalizada);
        var entregue = CriarOrdemServico(id: 3, status: StatusOrdemServico.Entregue);

        _ordemServicoRepositoryMock
            .Setup(r => r.ObterTodosAsync())
            .ReturnsAsync([recebida, finalizada, entregue]);

        // Act
        var resultado = (await _sut.ObterTodosAsync()).ToList();

        // Assert — apenas a OS "Recebida" deve aparecer na listagem
        Assert.Single(resultado);
        Assert.Equal(recebida.Id, resultado[0].Id);
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

    // ─── Monitoramento ───────────────────────────────────────────────────────────

    #region Monitoramento

    [Fact]
    public async Task AdicionarAsync_QuandoCriada_DeveRegistrarEventoDeCriacaoComDataDoStatus()
    {
        // Arrange
        var veiculo = CriarVeiculo();
        OrdemServico? gravada = null;

        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(veiculo);
        _ordemServicoRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<OrdemServico>()))
            .Callback<OrdemServico>(os => { os.Id = 10; gravada = os; })
            .ReturnsAsync((OrdemServico os) => os);
        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("10")).ReturnsAsync(() => gravada);

        // Act
        await _sut.AdicionarAsync("Barulho no motor", 500m, 1, 1);

        // Assert
        Assert.NotNull(gravada!.DataAlteracaoStatus);
        _monitoramentoMock.Verify(m => m.RegistrarOrdemServicoCriada(It.Is<OrdemServico>(os => os.Id == 10)), Times.Once);
    }

    [Fact]
    public async Task IniciarDiagnosticoAsync_DeveRegistrarTempoQueAOSFicouComoRecebida()
    {
        // Arrange
        var os = CriarOrdemServico(status: StatusOrdemServico.Recebida);
        os.DataAlteracaoStatus = DateTime.UtcNow.AddMinutes(-90);

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(os);
        _ordemServicoRepositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>())).ReturnsAsync(os);

        // Act
        await _sut.IniciarDiagnosticoAsync(1);

        // Assert
        _monitoramentoMock.Verify(m => m.RegistrarMudancaStatus(
            It.Is<OrdemServico>(o => o.Status == StatusOrdemServico.EmDiagnostico),
            StatusOrdemServico.Recebida,
            It.Is<TimeSpan>(t => t.TotalMinutes >= 89 && t.TotalMinutes <= 91)), Times.Once);

        Assert.True((DateTime.UtcNow - os.DataAlteracaoStatus!.Value).TotalSeconds < 5);
    }

    [Fact]
    public async Task FinalizarAsync_DeveRegistrarTempoEmExecucao()
    {
        // Arrange
        var os = CriarOrdemServico(status: StatusOrdemServico.EmExecucao);
        os.DataAlteracaoStatus = DateTime.UtcNow.AddHours(-3);

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(os);
        _ordemServicoRepositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>())).ReturnsAsync(os);

        // Act
        await _sut.FinalizarAsync(1);

        // Assert
        _monitoramentoMock.Verify(m => m.RegistrarMudancaStatus(
            It.IsAny<OrdemServico>(),
            StatusOrdemServico.EmExecucao,
            It.Is<TimeSpan>(t => t.TotalHours >= 2.9 && t.TotalHours <= 3.1)), Times.Once);
    }

    [Fact]
    public async Task EntregarAsync_SemDataDoStatus_DeveUsarDataDeCriacaoComoReferencia()
    {
        // OS gravada antes da coluna DataAlteracaoStatus existir.

        // Arrange
        var os = CriarOrdemServico(status: StatusOrdemServico.Finalizada);
        os.DataCriacao         = DateTime.UtcNow.AddDays(-2);
        os.DataAlteracaoStatus = null;

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(os);
        _ordemServicoRepositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>())).ReturnsAsync(os);

        // Act
        await _sut.EntregarAsync(1);

        // Assert
        _monitoramentoMock.Verify(m => m.RegistrarMudancaStatus(
            It.IsAny<OrdemServico>(),
            StatusOrdemServico.Finalizada,
            It.Is<TimeSpan>(t => t.TotalHours >= 47.9 && t.TotalHours <= 48.1)), Times.Once);
    }

    [Fact]
    public async Task RegistrarTransicao_ComDataDoStatusNoFuturo_NaoDeveRegistrarTempoNegativo()
    {
        // Arrange
        var os = CriarOrdemServico(status: StatusOrdemServico.Recebida);
        os.DataAlteracaoStatus = DateTime.UtcNow.AddHours(3);

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(os);
        _ordemServicoRepositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>())).ReturnsAsync(os);

        // Act
        await _sut.IniciarDiagnosticoAsync(1);

        // Assert
        _monitoramentoMock.Verify(m => m.RegistrarMudancaStatus(
            It.IsAny<OrdemServico>(), StatusOrdemServico.Recebida, TimeSpan.Zero), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_DevePreservarDatasDeCicloDeVida()
    {
        // O DTO de atualização não traz as datas; sem preservá-las, editar uma OS em
        // execução apagaria o início da execução e quebraria o tempo médio de execução.

        // Arrange
        var inicioExecucao = DateTime.UtcNow.AddHours(-5);
        var inicioStatus   = DateTime.UtcNow.AddHours(-5);

        var existente = CriarOrdemServico(status: StatusOrdemServico.EmExecucao);
        existente.DataInicioExecucao  = inicioExecucao;
        existente.DataAlteracaoStatus = inicioStatus;

        var atualizacao = CriarOrdemServico(status: StatusOrdemServico.Recebida);
        atualizacao.DescricaoProblema = "Barulho no motor e na suspensão";

        OrdemServico? gravada = null;
        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(existente);
        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(CriarVeiculo());
        _ordemServicoRepositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>()))
            .Callback<OrdemServico>(os => gravada = os)
            .ReturnsAsync((OrdemServico os) => os);

        // Act
        await _sut.AtualizarAsync(atualizacao);

        // Assert
        Assert.Equal(StatusOrdemServico.EmExecucao, gravada!.Status);
        Assert.Equal(inicioExecucao, gravada.DataInicioExecucao);
        Assert.Equal(inicioStatus, gravada.DataAlteracaoStatus);
        _monitoramentoMock.Verify(m => m.RegistrarMudancaStatus(
            It.IsAny<OrdemServico>(), It.IsAny<StatusOrdemServico>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoAvancaParaDiagnostico_DeveRegistrarTransicao()
    {
        // Arrange
        var existente = CriarOrdemServico(status: StatusOrdemServico.Recebida);
        existente.DataAlteracaoStatus = DateTime.UtcNow.AddMinutes(-30);

        var atualizacao = CriarOrdemServico(status: StatusOrdemServico.Recebida);

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(existente);
        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(CriarVeiculo());
        _ordemServicoRepositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>()))
            .ReturnsAsync((OrdemServico os) => os);

        // Act
        var resultado = await _sut.AtualizarAsync(atualizacao);

        // Assert
        Assert.Equal(StatusOrdemServico.EmDiagnostico, resultado.Status);
        _monitoramentoMock.Verify(m => m.RegistrarMudancaStatus(
            It.IsAny<OrdemServico>(),
            StatusOrdemServico.Recebida,
            It.Is<TimeSpan>(t => t.TotalMinutes >= 29 && t.TotalMinutes <= 31)), Times.Once);
    }

    [Fact]
    public async Task FinalizarAsync_QuandoRepositorioFalha_DeveRegistrarFalhaDeProcessamento()
    {
        // Arrange
        var os = CriarOrdemServico(status: StatusOrdemServico.EmExecucao);
        var falhaBanco = new TimeoutException("Timeout ao gravar a OS");

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(os);
        _ordemServicoRepositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>())).ThrowsAsync(falhaBanco);

        // Act
        await Assert.ThrowsAsync<TimeoutException>(() => _sut.FinalizarAsync(1));

        // Assert
        _monitoramentoMock.Verify(m => m.RegistrarFalhaProcessamento("Finalizar", 1, falhaBanco), Times.Once);
        _monitoramentoMock.Verify(m => m.RegistrarMudancaStatus(
            It.IsAny<OrdemServico>(), It.IsAny<StatusOrdemServico>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task IniciarDiagnosticoAsync_QuandoTransicaoInvalida_DeveRegistrarFalhaParaClassificacao()
    {
        // Arrange
        var os = CriarOrdemServico(status: StatusOrdemServico.EmExecucao);
        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(os);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.IniciarDiagnosticoAsync(1));

        // Assert
        _monitoramentoMock.Verify(m => m.RegistrarFalhaProcessamento(
            "IniciarDiagnostico", 1, It.IsAny<InvalidOperationException>()), Times.Once);
    }

    [Fact]
    public async Task IniciarDiagnosticoAsync_QuandoEmailFalha_DeveRegistrarFalhaDeIntegracaoSemInterromperATransicao()
    {
        // Arrange
        var os = CriarOrdemServico(status: StatusOrdemServico.Recebida);
        os.Cliente = new Cliente { Id = 1, Nome = "Fernanda Lima", Email = "fernanda@email.com", CpfCnpj = "52998224725", Ativo = true };
        var falhaSmtp = new IOException("SMTP indisponível");

        _ordemServicoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(os);
        _ordemServicoRepositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<OrdemServico>())).ReturnsAsync(os);
        _emailServiceMock
            .Setup(e => e.EnviarEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(falhaSmtp);

        // Act
        var resultado = await _sut.IniciarDiagnosticoAsync(1);

        // Assert
        Assert.Equal(StatusOrdemServico.EmDiagnostico, resultado.Status);
        _monitoramentoMock.Verify(m => m.RegistrarFalhaIntegracao("Email", "NotificacaoStatus", falhaSmtp), Times.Once);
    }

    #endregion
}
