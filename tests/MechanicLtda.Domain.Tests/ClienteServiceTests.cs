using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Domain.Interfaces.Services;
using MechanicLtda.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MechanicLtda.Domain.Tests;

public class ClienteServiceTests
{
    private readonly Mock<IClienteRepository> _repositoryMock;
    private readonly Mock<INotificadorService> _notificadorMock;
    private readonly Mock<ILogger<ClienteService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly ClienteService _sut;

    public ClienteServiceTests()
    {
        _repositoryMock = new Mock<IClienteRepository>();
        _notificadorMock = new Mock<INotificadorService>();
        _loggerMock = new Mock<ILogger<ClienteService>>();
        _configurationMock = new Mock<IConfiguration>();

        _sut = new ClienteService(
            _loggerMock.Object,
            _configurationMock.Object,
            _notificadorMock.Object,
            _repositoryMock.Object);
    }

    #region AdicionarAsync

    [Fact]
    public async Task AdicionarAsync_QuandoEmailNaoExiste_DeveRetornarClienteCriado()
    {
        // Arrange
        var nome = "João Silva";
        var email = "joao@email.com";
        var telefone = "11999999999";

        var clienteEsperado = new Cliente
        {
            Id = 1,
            Nome = nome,
            Email = email,
            Telefone = telefone,
            Ativo = true,
            DataCriacao = DateTime.Now
        };

        _repositoryMock
            .Setup(r => r.EmailExisteAsync(email))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Cliente>()))
            .ReturnsAsync(clienteEsperado);

        // Act
        var resultado = await _sut.AdicionarAsync(nome, email, telefone);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(email, resultado.Email);
        Assert.Equal(nome, resultado.Nome);
        Assert.True(resultado.Ativo);
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Cliente>()), Times.Once);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoEmailJaExiste_DeveLancarInvalidOperationException()
    {
        // Arrange
        var email = "duplicado@email.com";

        _repositoryMock
            .Setup(r => r.EmailExisteAsync(email))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AdicionarAsync("Nome Qualquer", email, null));

        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Cliente>()), Times.Never);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoTelefoneNulo_DeveRetornarClienteCriado()
    {
        // Arrange
        var nome = "Maria";
        var email = "maria@email.com";

        var clienteEsperado = new Cliente { Id = 2, Nome = nome, Email = email, Ativo = true };

        _repositoryMock.Setup(r => r.EmailExisteAsync(email)).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AdicionarAsync(It.IsAny<Cliente>())).ReturnsAsync(clienteEsperado);

        // Act
        var resultado = await _sut.AdicionarAsync(nome, email, null);

        // Assert
        Assert.NotNull(resultado);
        Assert.Null(resultado.Telefone);
    }

    #endregion

    #region AtualizarAsync

    [Fact]
    public async Task AtualizarAsync_QuandoClienteExisteEEmailDisponivel_DeveRetornarClienteAtualizado()
    {
        // Arrange
        var id = 1;
        var cliente = new Cliente
        {
            Id = id,
            Nome = "Novo Nome",
            Email = "novo@email.com",
            Ativo = true
        };

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(id.ToString()))
            .ReturnsAsync(new Cliente { Id = id, Nome = "Antigo", Email = "antigo@email.com", DataCriacao = DateTime.Now });

        _repositoryMock
            .Setup(r => r.ObterPorEmailAsync(cliente.Email))
            .ReturnsAsync((Cliente?)null);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<Cliente>()))
            .ReturnsAsync(cliente);

        // Act
        var resultado = await _sut.AtualizarAsync(cliente);

        // Assert
        Assert.NotNull(resultado);
        Assert.NotNull(cliente.DataModificacao);
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Cliente>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoClienteNaoEncontrado_DeveLancarKeyNotFoundException()
    {
        // Arrange
        var cliente = new Cliente { Id = 99, Email = "x@email.com" };

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(cliente.Id.ToString()))
            .ReturnsAsync((Cliente?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.AtualizarAsync(cliente));

        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Cliente>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoEmailEmUsoDeOutroCliente_DeveLancarInvalidOperationException()
    {
        // Arrange
        var id = 1;
        var outroId = 2;
        var emailEmUso = "emuso@email.com";

        var cliente = new Cliente { Id = id, Email = emailEmUso };
        var outroCliente = new Cliente { Id = outroId, Email = emailEmUso };

        _repositoryMock.Setup(r => r.ObterPorIdAsync(id.ToString())).ReturnsAsync(cliente);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(emailEmUso)).ReturnsAsync(outroCliente);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AtualizarAsync(cliente));
    }

    [Fact]
    public async Task AtualizarAsync_QuandoMesmoEmailDoProprioCliente_DeveAtualizarSemErro()
    {
        // Arrange
        var id = 1;
        var email = "mesmo@email.com";
        var cliente = new Cliente { Id = id, Nome = "Atualizado", Email = email, Ativo = true };

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(id.ToString()))
            .ReturnsAsync(new Cliente { Id = id, Email = email, DataCriacao = DateTime.Now });

        _repositoryMock
            .Setup(r => r.ObterPorEmailAsync(email))
            .ReturnsAsync(cliente); // mesmo cliente

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<Cliente>()))
            .ReturnsAsync(cliente);

        // Act
        var resultado = await _sut.AtualizarAsync(cliente);

        // Assert
        Assert.NotNull(resultado);
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Cliente>()), Times.Once);
    }

    #endregion

    #region ObterTodosAsync

    [Fact]
    public async Task ObterTodosAsync_DeveRetornarListaDeClientes()
    {
        // Arrange
        var lista = new List<Cliente>
        {
            new() { Id = 1, Nome = "Cliente 1", Email = "c1@email.com" },
            new() { Id = 2, Nome = "Cliente 2", Email = "c2@email.com" }
        };

        _repositoryMock.Setup(r => r.ObterTodosAsync()).ReturnsAsync(lista);

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
        _repositoryMock
            .Setup(r => r.ObterTodosAsync())
            .ThrowsAsync(new Exception("Erro no banco"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.ObterTodosAsync());
    }

    #endregion

    #region ObterPorIdAsync

    [Fact]
    public async Task ObterPorIdAsync_QuandoClienteExiste_DeveRetornarCliente()
    {
        // Arrange
        var id = 1;
        var cliente = new Cliente { Id = id, Nome = "João", Email = "joao@email.com" };

        _repositoryMock.Setup(r => r.ObterPorIdAsync(id.ToString())).ReturnsAsync(cliente);

        // Act
        var resultado = await _sut.ObterPorIdAsync(id.ToString());

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(id, resultado!.Id);
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoClienteNaoExiste_DeveRetornarNull()
    {
        // Arrange
        var id = "999";

        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync((Cliente?)null);

        // Act
        var resultado = await _sut.ObterPorIdAsync(id);

        // Assert
        Assert.Null(resultado);
    }

    #endregion

    #region RemoverAsync

    [Fact]
    public async Task RemoverAsync_QuandoClienteExiste_DeveChamarRepositorio()
    {
        // Arrange
        var id = "1";
        var cliente = new Cliente { Id = 1 };

        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(cliente);
        _repositoryMock.Setup(r => r.RemoverAsync(id)).Returns(Task.CompletedTask);

        // Act
        await _sut.RemoverAsync(id);

        // Assert
        _repositoryMock.Verify(r => r.RemoverAsync(id), Times.Once);
    }

    [Fact]
    public async Task RemoverAsync_QuandoClienteNaoExiste_DeveLancarKeyNotFoundException()
    {
        // Arrange
        var id = "999";

        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync((Cliente?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.RemoverAsync(id));

        _repositoryMock.Verify(r => r.RemoverAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion
}