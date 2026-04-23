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

    private static OrdemServico CriarEntidade(int id = 1, StatusOrdemServico status = StatusOrdemServico.EmAberto) =>
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

    private static OrdemServicoDto CriarDto(int id = 1, StatusOrdemServico status = StatusOrdemServico.EmAberto) =>
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
        var createDto  = new OrdemServicoCreateDto { DescricaoProblema = "Barulho no motor", ValorTotalEstimado = 500m, VeiculoId = 1, ClienteId = 1 };
        var entidade   = CriarEntidade();
        var ordemDto   = CriarDto();

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

    // ─── MoverParaEmValidacaoAsync ──────────────────────────────────────────────

    #region MoverParaEmValidacaoAsync

    [Fact]
    public async Task MoverParaEmValidacaoAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var entidade = CriarEntidade(status: StatusOrdemServico.EmValidacao);
        var ordemDto = CriarDto(status: StatusOrdemServico.EmValidacao);

        _serviceMock
            .Setup(s => s.MoverParaEmValidacaoAsync(1))
            .ReturnsAsync(entidade);

        _mapperMock
            .Setup(m => m.Map<OrdemServicoDto>(entidade))
            .Returns(ordemDto);

        // Act
        var response = await _sut.MoverParaEmValidacaoAsync("1");

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(StatusOrdemServico.EmValidacao, response.getResponse.Status);
    }

    [Fact]
    public async Task MoverParaEmValidacaoAsync_QuandoIdInvalido_DeveRetornarResponseComErro()
    {
        // Act
        var response = await _sut.MoverParaEmValidacaoAsync("nao-e-numero");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
        _serviceMock.Verify(s => s.MoverParaEmValidacaoAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task MoverParaEmValidacaoAsync_QuandoOSNaoEncontrada_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.MoverParaEmValidacaoAsync(99))
            .ThrowsAsync(new KeyNotFoundException("Ordem de Serviço com Id '99' não encontrada."));

        // Act
        var response = await _sut.MoverParaEmValidacaoAsync("99");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    [Fact]
    public async Task MoverParaEmValidacaoAsync_QuandoOSJaEmValidacao_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.MoverParaEmValidacaoAsync(1))
            .ThrowsAsync(new InvalidOperationException("A OS só pode ser movida para 'Em Validação' quando estiver 'Em Aberto'."));

        // Act
        var response = await _sut.MoverParaEmValidacaoAsync("1");

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