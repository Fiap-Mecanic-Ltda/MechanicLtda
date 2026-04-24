using AutoMapper;
using MechanicLtda.Application.AppServices;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;
using MechanicLtda.Domain.Interfaces.Services;
using Moq;
using Xunit;

namespace MechanicLtda.Application.Tests;

public class OrdemServicoAppServiceTests
{
    private readonly Mock<IOrdemServicoService> _serviceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly OrdemServicoAppService _sut;

    public OrdemServicoAppServiceTests()
    {
        _serviceMock = new Mock<IOrdemServicoService>();
        _mapperMock  = new Mock<IMapper>();
        _sut = new OrdemServicoAppService(_serviceMock.Object, _mapperMock.Object);
    }

    // ─── helpers ────────────────────────────────────────────────────────────────

    private static OrdemServico CriarEntidade(int id = 1, StatusOrdemServico status = StatusOrdemServico.Recebida) =>
        new()
        {
            Id                 = id,
            DescricaoProblema  = "Barulho no motor",
            ValorTotalEstimado = 500m,
            VeiculoId          = 1,
            ClienteId          = 1,
            Status             = status,
            DataCriacao        = DateTime.Now
        };

    private static OrdemServicoDto CriarDto(int id = 1, StatusOrdemServico status = StatusOrdemServico.Recebida) =>
        new()
        {
            Id                 = id,
            DescricaoProblema  = "Barulho no motor",
            ValorTotalEstimado = 500m,
            VeiculoId          = 1,
            ClienteId          = 1,
            Status             = status,
            DataCriacao        = DateTime.Now
        };

    // ─── AdicionarAsync ─────────────────────────────────────────────────────────

    #region AdicionarAsync

    [Fact]
    public async Task AdicionarAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var createDto = new OrdemServicoCreateDto { DescricaoProblema = "Barulho no motor", ValorTotalEstimado = 500m, VeiculoId = 1, ClienteId = 1 };
        var entidade  = CriarEntidade();
        var ordemDto  = CriarDto();

        _serviceMock
            .Setup(s => s.AdicionarAsync(createDto.DescricaoProblema, createDto.ValorTotalEstimado, createDto.VeiculoId, createDto.ClienteId))
            .ReturnsAsync(entidade);

        _mapperMock
            .Setup(m => m.Map<OrdemServicoDto>(entidade))
            .Returns(ordemDto);

        // Act
        var response = await _sut.AdicionarAsync(createDto);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(ordemDto, response.getResponse);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoVeiculoNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        var createDto = new OrdemServicoCreateDto { DescricaoProblema = "Descrição", VeiculoId = 99, ClienteId = 1 };

        _serviceMock
            .Setup(s => s.AdicionarAsync(It.IsAny<string>(), It.IsAny<decimal?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new KeyNotFoundException("Veículo com Id '99' não encontrado."));

        // Act
        var response = await _sut.AdicionarAsync(createDto);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoVeiculoNaoPertenceAoCliente_DeveRetornarResponseComErro()
    {
        // Arrange
        var createDto = new OrdemServicoCreateDto { DescricaoProblema = "Descrição", VeiculoId = 1, ClienteId = 99 };

        _serviceMock
            .Setup(s => s.AdicionarAsync(It.IsAny<string>(), It.IsAny<decimal?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("O veículo informado não pertence ao cliente indicado."));

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
        var id        = "1";
        var updateDto = new OrdemServicoUpdateDto { DescricaoProblema = "Atualizado", ValorTotalEstimado = 750m, VeiculoId = 1, ClienteId = 1 };
        var entidade  = CriarEntidade();
        var ordemDto  = CriarDto();

        _mapperMock
            .Setup(m => m.Map<OrdemServico>(updateDto))
            .Returns(entidade);

        _serviceMock
            .Setup(s => s.AtualizarAsync(It.IsAny<OrdemServico>()))
            .ReturnsAsync(entidade);

        _mapperMock
            .Setup(m => m.Map<OrdemServicoDto>(entidade))
            .Returns(ordemDto);

        // Act
        var response = await _sut.AtualizarAsync(id, updateDto);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(ordemDto, response.getResponse);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoOSNaoEncontrada_DeveRetornarResponseComErro()
    {
        // Arrange
        var id        = "999";
        var updateDto = new OrdemServicoUpdateDto { DescricaoProblema = "Descrição", VeiculoId = 1, ClienteId = 1 };

        _mapperMock
            .Setup(m => m.Map<OrdemServico>(updateDto))
            .Returns(new OrdemServico());

        _serviceMock
            .Setup(s => s.AtualizarAsync(It.IsAny<OrdemServico>()))
            .ThrowsAsync(new KeyNotFoundException($"Ordem de Serviço com Id '{id}' não encontrada."));

        // Act
        var response = await _sut.AtualizarAsync(id, updateDto);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoVeiculoNaoPertenceAoCliente_DeveRetornarResponseComErro()
    {
        // Arrange
        var updateDto = new OrdemServicoUpdateDto { DescricaoProblema = "Descrição", VeiculoId = 1, ClienteId = 99 };

        _mapperMock
            .Setup(m => m.Map<OrdemServico>(updateDto))
            .Returns(new OrdemServico());

        _serviceMock
            .Setup(s => s.AtualizarAsync(It.IsAny<OrdemServico>()))
            .ThrowsAsync(new InvalidOperationException("O veículo informado não pertence ao cliente indicado."));

        // Act
        var response = await _sut.AtualizarAsync("1", updateDto);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    // ─── IniciarDiagnosticoAsync ─────────────────────────────────────────────────

    #region IniciarDiagnosticoAsync

    [Fact]
    public async Task IniciarDiagnosticoAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var entidade = CriarEntidade(status: StatusOrdemServico.EmDiagnostico);
        var ordemDto = CriarDto(status: StatusOrdemServico.EmDiagnostico);

        _serviceMock
            .Setup(s => s.IniciarDiagnosticoAsync(1))
            .ReturnsAsync(entidade);

        _mapperMock
            .Setup(m => m.Map<OrdemServicoDto>(entidade))
            .Returns(ordemDto);

        // Act
        var response = await _sut.IniciarDiagnosticoAsync("1");

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(StatusOrdemServico.EmDiagnostico, response.getResponse.Status);
    }

    [Fact]
    public async Task IniciarDiagnosticoAsync_QuandoIdInvalido_DeveRetornarResponseComErro()
    {
        // Act
        var response = await _sut.IniciarDiagnosticoAsync("nao-e-numero");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
        _serviceMock.Verify(s => s.IniciarDiagnosticoAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task IniciarDiagnosticoAsync_QuandoOSNaoEncontrada_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.IniciarDiagnosticoAsync(99))
            .ThrowsAsync(new KeyNotFoundException("Ordem de Serviço com Id '99' não encontrada."));

        // Act
        var response = await _sut.IniciarDiagnosticoAsync("99");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    [Fact]
    public async Task IniciarDiagnosticoAsync_QuandoStatusInvalido_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.IniciarDiagnosticoAsync(1))
            .ThrowsAsync(new InvalidOperationException("A OS só pode ir para 'Em Diagnóstico' quando estiver 'Recebida'."));

        // Act
        var response = await _sut.IniciarDiagnosticoAsync("1");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    // ─── AguardarAprovacaoAsync ──────────────────────────────────────────────────

    #region AguardarAprovacaoAsync

    [Fact]
    public async Task AguardarAprovacaoAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var entidade = CriarEntidade(status: StatusOrdemServico.AguardandoAprovacao);
        var ordemDto = CriarDto(status: StatusOrdemServico.AguardandoAprovacao);

        _serviceMock
            .Setup(s => s.AguardarAprovacaoAsync(1))
            .ReturnsAsync(entidade);

        _mapperMock
            .Setup(m => m.Map<OrdemServicoDto>(entidade))
            .Returns(ordemDto);

        // Act
        var response = await _sut.AguardarAprovacaoAsync("1");

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, response.getResponse.Status);
    }

    [Fact]
    public async Task AguardarAprovacaoAsync_QuandoIdInvalido_DeveRetornarResponseComErro()
    {
        // Act
        var response = await _sut.AguardarAprovacaoAsync("nao-e-numero");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
        _serviceMock.Verify(s => s.AguardarAprovacaoAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AguardarAprovacaoAsync_QuandoOSNaoEncontrada_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.AguardarAprovacaoAsync(99))
            .ThrowsAsync(new KeyNotFoundException("Ordem de Serviço com Id '99' não encontrada."));

        // Act
        var response = await _sut.AguardarAprovacaoAsync("99");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    [Fact]
    public async Task AguardarAprovacaoAsync_QuandoStatusInvalido_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.AguardarAprovacaoAsync(1))
            .ThrowsAsync(new InvalidOperationException("A OS só pode ir para 'Aguardando Aprovação' quando estiver 'Em Diagnóstico'."));

        // Act
        var response = await _sut.AguardarAprovacaoAsync("1");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    // ─── IniciarExecucaoAsync ────────────────────────────────────────────────────

    #region IniciarExecucaoAsync

    [Fact]
    public async Task IniciarExecucaoAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var entidade = CriarEntidade(status: StatusOrdemServico.EmExecucao);
        var ordemDto = CriarDto(status: StatusOrdemServico.EmExecucao);

        _serviceMock
            .Setup(s => s.IniciarExecucaoAsync(1))
            .ReturnsAsync(entidade);

        _mapperMock
            .Setup(m => m.Map<OrdemServicoDto>(entidade))
            .Returns(ordemDto);

        // Act
        var response = await _sut.IniciarExecucaoAsync("1");

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(StatusOrdemServico.EmExecucao, response.getResponse.Status);
    }

    [Fact]
    public async Task IniciarExecucaoAsync_QuandoIdInvalido_DeveRetornarResponseComErro()
    {
        // Act
        var response = await _sut.IniciarExecucaoAsync("nao-e-numero");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
        _serviceMock.Verify(s => s.IniciarExecucaoAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task IniciarExecucaoAsync_QuandoOSNaoEncontrada_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.IniciarExecucaoAsync(99))
            .ThrowsAsync(new KeyNotFoundException("Ordem de Serviço com Id '99' não encontrada."));

        // Act
        var response = await _sut.IniciarExecucaoAsync("99");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    [Fact]
    public async Task IniciarExecucaoAsync_QuandoStatusInvalido_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.IniciarExecucaoAsync(1))
            .ThrowsAsync(new InvalidOperationException("A OS só pode ir para 'Em Execução' quando estiver 'Aguardando Aprovação'."));

        // Act
        var response = await _sut.IniciarExecucaoAsync("1");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    // ─── FinalizarAsync ──────────────────────────────────────────────────────────

    #region FinalizarAsync

    [Fact]
    public async Task FinalizarAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var entidade = CriarEntidade(status: StatusOrdemServico.Finalizada);
        var ordemDto = CriarDto(status: StatusOrdemServico.Finalizada);

        _serviceMock
            .Setup(s => s.FinalizarAsync(1))
            .ReturnsAsync(entidade);

        _mapperMock
            .Setup(m => m.Map<OrdemServicoDto>(entidade))
            .Returns(ordemDto);

        // Act
        var response = await _sut.FinalizarAsync("1");

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(StatusOrdemServico.Finalizada, response.getResponse.Status);
    }

    [Fact]
    public async Task FinalizarAsync_QuandoIdInvalido_DeveRetornarResponseComErro()
    {
        // Act
        var response = await _sut.FinalizarAsync("nao-e-numero");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
        _serviceMock.Verify(s => s.FinalizarAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task FinalizarAsync_QuandoOSNaoEncontrada_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.FinalizarAsync(99))
            .ThrowsAsync(new KeyNotFoundException("Ordem de Serviço com Id '99' não encontrada."));

        // Act
        var response = await _sut.FinalizarAsync("99");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    [Fact]
    public async Task FinalizarAsync_QuandoStatusInvalido_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.FinalizarAsync(1))
            .ThrowsAsync(new InvalidOperationException("A OS só pode ser 'Finalizada' quando estiver 'Em Execução'."));

        // Act
        var response = await _sut.FinalizarAsync("1");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    // ─── EntregarAsync ───────────────────────────────────────────────────────────

    #region EntregarAsync

    [Fact]
    public async Task EntregarAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var entidade = CriarEntidade(status: StatusOrdemServico.Entregue);
        var ordemDto = CriarDto(status: StatusOrdemServico.Entregue);

        _serviceMock
            .Setup(s => s.EntregarAsync(1))
            .ReturnsAsync(entidade);

        _mapperMock
            .Setup(m => m.Map<OrdemServicoDto>(entidade))
            .Returns(ordemDto);

        // Act
        var response = await _sut.EntregarAsync("1");

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(StatusOrdemServico.Entregue, response.getResponse.Status);
    }

    [Fact]
    public async Task EntregarAsync_QuandoIdInvalido_DeveRetornarResponseComErro()
    {
        // Act
        var response = await _sut.EntregarAsync("nao-e-numero");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
        _serviceMock.Verify(s => s.EntregarAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task EntregarAsync_QuandoOSNaoEncontrada_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.EntregarAsync(99))
            .ThrowsAsync(new KeyNotFoundException("Ordem de Serviço com Id '99' não encontrada."));

        // Act
        var response = await _sut.EntregarAsync("99");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    [Fact]
    public async Task EntregarAsync_QuandoStatusInvalido_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.EntregarAsync(1))
            .ThrowsAsync(new InvalidOperationException("A OS só pode ser 'Entregue' quando estiver 'Finalizada'."));

        // Act
        var response = await _sut.EntregarAsync("1");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    // ─── ObterTodosAsync ────────────────────────────────────────────────────────

    #region ObterTodosAsync

    [Fact]
    public async Task ObterTodosAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var entidades = new List<OrdemServico> { CriarEntidade(1), CriarEntidade(2) };
        var dtos      = new List<OrdemServicoDto> { CriarDto(1), CriarDto(2) };

        _serviceMock.Setup(s => s.ObterTodosAsync()).ReturnsAsync(entidades);
        _mapperMock.Setup(m => m.Map<IEnumerable<OrdemServicoDto>>(entidades)).Returns(dtos);

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

    // ─── ObterPorClienteIdAsync ─────────────────────────────────────────────────

    #region ObterPorClienteIdAsync

    [Fact]
    public async Task ObterPorClienteIdAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var clienteId = "1";
        var entidades = new List<OrdemServico> { CriarEntidade(1), CriarEntidade(2) };
        var dtos      = new List<OrdemServicoDto> { CriarDto(1), CriarDto(2) };

        _serviceMock.Setup(s => s.ObterPorClienteIdAsync(clienteId)).ReturnsAsync(entidades);
        _mapperMock.Setup(m => m.Map<IEnumerable<OrdemServicoDto>>(entidades)).Returns(dtos);

        // Act
        var response = await _sut.ObterPorClienteIdAsync(clienteId);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(2, response.getResponse.Count());
    }

    [Fact]
    public async Task ObterPorClienteIdAsync_QuandoServicoLancaExcecao_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.ObterPorClienteIdAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Erro ao consultar ordens do cliente"));

        // Act
        var response = await _sut.ObterPorClienteIdAsync("99");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    // ─── ObterPorIdAsync ────────────────────────────────────────────────────────

    #region ObterPorIdAsync

    [Fact]
    public async Task ObterPorIdAsync_QuandoOSEncontrada_DeveRetornarResponseSemErros()
    {
        // Arrange
        var id       = "1";
        var entidade = CriarEntidade();
        var ordemDto = CriarDto();

        _serviceMock.Setup(s => s.ObterPorIdAsync(id)).ReturnsAsync(entidade);
        _mapperMock.Setup(m => m.Map<OrdemServicoDto>(entidade)).Returns(ordemDto);

        // Act
        var response = await _sut.ObterPorIdAsync(id);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(ordemDto, response.getResponse);
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoOSNaoEncontrada_DeveRetornarResponseComErro()
    {
        // Arrange
        var id = "999";

        _serviceMock.Setup(s => s.ObterPorIdAsync(id)).ReturnsAsync((OrdemServico?)null);

        // Act
        var response = await _sut.ObterPorIdAsync(id);

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
    public async Task RemoverAsync_QuandoOSNaoEncontrada_DeveRetornarResponseComErro()
    {
        // Arrange
        var id = "999";

        _serviceMock
            .Setup(s => s.RemoverAsync(id))
            .ThrowsAsync(new KeyNotFoundException($"Ordem de Serviço com Id '{id}' não encontrada."));

        // Act
        var response = await _sut.RemoverAsync(id);

        // Assert
        Assert.True(response.hasErrors);
        Assert.False(response.getResponse);
    }

    #endregion
}