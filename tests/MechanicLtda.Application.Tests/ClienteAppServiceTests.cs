using AutoMapper;
using MechanicLtda.Application.AppServices;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Services;
using Moq;
using Xunit;

namespace MechanicLtda.Application.Tests;

public class ClienteAppServiceTests
{
    private readonly Mock<IClienteService> _serviceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly ClienteAppService _sut;

    public ClienteAppServiceTests()
    {
        _serviceMock = new Mock<IClienteService>();
        _mapperMock = new Mock<IMapper>();
        _sut = new ClienteAppService(_serviceMock.Object, _mapperMock.Object);
    }

    #region AdicionarAsync

    [Fact]
    public async Task AdicionarAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var dto = new ClienteCreateDto { Nome = "João", Email = "joao@email.com", Telefone = "11999999999", CpfCnpj = "12345678901" };

        var clienteCriado = new Cliente { Id = 1, Nome = dto.Nome, Email = dto.Email, CpfCnpj = dto.CpfCnpj };
        var clienteDto = new ClienteDto { Nome = dto.Nome, Email = dto.Email, CpfCnpj = dto.CpfCnpj };

        _serviceMock
            .Setup(s => s.AdicionarAsync(dto.Nome, dto.Email, dto.Telefone, dto.CpfCnpj))
            .ReturnsAsync(clienteCriado);

        _mapperMock
            .Setup(m => m.Map<ClienteDto>(clienteCriado))
            .Returns(clienteDto);

        // Act
        var response = await _sut.AdicionarAsync(dto);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(clienteDto, response.getResponse);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoEmailJaCadastrado_DeveRetornarResponseComErro()
    {
        // Arrange
        var dto = new ClienteCreateDto { Nome = "Fail", Email = "fail@email.com", CpfCnpj = "12345678901" };

        _serviceMock
            .Setup(s => s.AdicionarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("E-mail já cadastrado."));

        // Act
        var response = await _sut.AdicionarAsync(dto);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    #region AtualizarAsync

    [Fact]
    public async Task AtualizarAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var id = 1;
        var dto = new ClienteUpdateDto { Nome = "Atualizado", Email = "atualizado@email.com", CpfCnpj = "12345678901", Ativo = true };

        var entidade = new Cliente { Id = id, Nome = dto.Nome, Email = dto.Email, CpfCnpj = dto.CpfCnpj };
        var clienteDto = new ClienteDto { Nome = dto.Nome, Email = dto.Email, CpfCnpj = dto.CpfCnpj };

        _mapperMock.Setup(m => m.Map<Cliente>(dto)).Returns(entidade);
        _serviceMock.Setup(s => s.AtualizarAsync(It.IsAny<Cliente>())).ReturnsAsync(entidade);
        _mapperMock.Setup(m => m.Map<ClienteDto>(entidade)).Returns(clienteDto);

        // Act
        var response = await _sut.AtualizarAsync(id.ToString(), dto);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(clienteDto, response.getResponse);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoClienteNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        var id = 99;
        var dto = new ClienteUpdateDto { Nome = "x", Email = "x@email.com", CpfCnpj = "12345678901" };

        _mapperMock.Setup(m => m.Map<Cliente>(dto)).Returns(new Cliente());
        _serviceMock
            .Setup(s => s.AtualizarAsync(It.IsAny<Cliente>()))
            .ThrowsAsync(new KeyNotFoundException("Cliente não encontrado."));

        // Act
        var response = await _sut.AtualizarAsync(id.ToString(), dto);

        // Assert
        Assert.True(response.hasErrors);
    }

    #endregion

    #region ObterTodosAsync

    [Fact]
    public async Task ObterTodosAsync_QuandoExistemClientes_DeveRetornarListaMapeada()
    {
        // Arrange
        var clientes = new List<Cliente>
        {
            new() { Id = 1, Nome = "c1", Email = "c1@email.com", CpfCnpj = "11111111111" },
            new() { Id = 2, Nome = "c2", Email = "c2@email.com", CpfCnpj = "22222222222" }
        };

        var dtos = clientes.Select(c => new ClienteDto { Nome = c.Nome, Email = c.Email, CpfCnpj = c.CpfCnpj });

        _serviceMock.Setup(s => s.ObterTodosAsync()).ReturnsAsync(clientes);
        _mapperMock.Setup(m => m.Map<IEnumerable<ClienteDto>>(clientes)).Returns(dtos);

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
            .ThrowsAsync(new Exception("Erro inesperado"));

        // Act
        var response = await _sut.ObterTodosAsync();

        // Assert
        Assert.True(response.hasErrors);
    }

    #endregion

    #region ObterPorIdAsync

    [Fact]
    public async Task ObterPorIdAsync_QuandoClienteExiste_DeveRetornarResponseSemErros()
    {
        // Arrange
        var id = 1;
        var cliente = new Cliente { Id = id, Nome = "João", Email = "joao@email.com", CpfCnpj = "12345678901" };
        var dto = new ClienteDto { Id = id, Nome = "João", Email = "joao@email.com", CpfCnpj = "12345678901" };

        _serviceMock.Setup(s => s.ObterPorIdAsync(id.ToString())).ReturnsAsync(cliente);
        _mapperMock.Setup(m => m.Map<ClienteDto>(cliente)).Returns(dto);

        // Act
        var response = await _sut.ObterPorIdAsync(id.ToString());

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(dto, response.getResponse);
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoClienteNaoExiste_DeveRetornarResponseComErro()
    {
        // Arrange
        var id = "999";

        _serviceMock.Setup(s => s.ObterPorIdAsync(id)).ReturnsAsync((Cliente?)null);

        // Act
        var response = await _sut.ObterPorIdAsync(id);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    #region RemoverAsync

    [Fact]
    public async Task RemoverAsync_QuandoSucesso_DeveRetornarTrue()
    {
        // Arrange
        var id = "1";
        _serviceMock.Setup(s => s.RemoverAsync(id)).Returns(Task.CompletedTask);

        // Act
        var response = await _sut.RemoverAsync(id);

        // Assert
        Assert.False(response.hasErrors);
        Assert.True(response.getResponse);
    }

    [Fact]
    public async Task RemoverAsync_QuandoClienteNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        var id = "999";
        _serviceMock
            .Setup(s => s.RemoverAsync(id))
            .ThrowsAsync(new KeyNotFoundException("Cliente não encontrado."));

        // Act
        var response = await _sut.RemoverAsync(id);

        // Assert
        Assert.True(response.hasErrors);
        Assert.False(response.getResponse);
    }

    #endregion

    // ─── Dados Sensíveis — CpfCnpj ───────────────────────────────────────────────

    #region DadosSensiveis_CpfCnpj

    [Fact]
    public async Task AdicionarAsync_DevePropagarlCpfCnpjParaOServicoDeDominio()
    {
        // Arrange
        var cpfCnpj = "52998224725";
        var dto = new ClienteCreateDto { Nome = "João", Email = "joao@email.com", CpfCnpj = cpfCnpj };

        string cpfPassado = null!;
        _serviceMock
            .Setup(s => s.AdicionarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>()))
            .Callback<string, string, string?, string>((_, _, _, cpf) => cpfPassado = cpf)
            .ReturnsAsync(new Cliente { Nome = dto.Nome, Email = dto.Email, CpfCnpj = cpfCnpj });

        _mapperMock
            .Setup(m => m.Map<ClienteDto>(It.IsAny<Cliente>()))
            .Returns(new ClienteDto { CpfCnpj = cpfCnpj });

        // Act
        await _sut.AdicionarAsync(dto);

        // Assert
        Assert.Equal(cpfCnpj, cpfPassado);
    }

    [Fact]
    public async Task AdicionarAsync_ComCnpj_DevePropagarlCnpjParaOServicoDeDominio()
    {
        // Arrange
        var cnpj = "11222333000181";
        var dto = new ClienteCreateDto { Nome = "Empresa", Email = "emp@email.com", CpfCnpj = cnpj };

        string cnpjPassado = null!;
        _serviceMock
            .Setup(s => s.AdicionarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>()))
            .Callback<string, string, string?, string>((_, _, _, cpfCnpj) => cnpjPassado = cpfCnpj)
            .ReturnsAsync(new Cliente { Nome = dto.Nome, Email = dto.Email, CpfCnpj = cnpj });

        _mapperMock
            .Setup(m => m.Map<ClienteDto>(It.IsAny<Cliente>()))
            .Returns(new ClienteDto { CpfCnpj = cnpj });

        // Act
        await _sut.AdicionarAsync(dto);

        // Assert
        Assert.Equal(cnpj, cnpjPassado);
    }

    [Fact]
    public async Task AdicionarAsync_ResponseDeveConterCpfCnpjMapeadoCorretamente()
    {
        // Arrange
        var cpfCnpj = "52998224725";
        var dto = new ClienteCreateDto { Nome = "Ana", Email = "ana@email.com", CpfCnpj = cpfCnpj };
        var entidade = new Cliente { Nome = dto.Nome, Email = dto.Email, CpfCnpj = cpfCnpj };
        var clienteDto = new ClienteDto { Nome = dto.Nome, Email = dto.Email, CpfCnpj = cpfCnpj };

        _serviceMock
            .Setup(s => s.AdicionarAsync(dto.Nome, dto.Email, dto.Telefone, dto.CpfCnpj))
            .ReturnsAsync(entidade);

        _mapperMock
            .Setup(m => m.Map<ClienteDto>(entidade))
            .Returns(clienteDto);

        // Act
        var response = await _sut.AdicionarAsync(dto);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(cpfCnpj, response.getResponse.CpfCnpj);
    }

    [Fact]
    public async Task AtualizarAsync_DevePropagarlCpfCnpjAtualizadoParaOServicoDeDominio()
    {
        // Arrange
        var cpfCnpj = "11144477735";
        var dto = new ClienteUpdateDto { Nome = "Atualizado", Email = "at@email.com", CpfCnpj = cpfCnpj, Ativo = true };
        var entidade = new Cliente { Id = 1, Nome = dto.Nome, Email = dto.Email, CpfCnpj = cpfCnpj };

        _mapperMock.Setup(m => m.Map<Cliente>(dto)).Returns(entidade);

        Cliente clientePassado = null!;
        _serviceMock
            .Setup(s => s.AtualizarAsync(It.IsAny<Cliente>()))
            .Callback<Cliente>(c => clientePassado = c)
            .ReturnsAsync(entidade);

        _mapperMock
            .Setup(m => m.Map<ClienteDto>(entidade))
            .Returns(new ClienteDto { CpfCnpj = cpfCnpj });

        // Act
        await _sut.AtualizarAsync("1", dto);

        // Assert
        Assert.Equal(cpfCnpj, clientePassado.CpfCnpj);
    }

    #endregion
}