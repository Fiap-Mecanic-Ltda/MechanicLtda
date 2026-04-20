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

public class UsuarioServiceTests
{
    private readonly Mock<IUsuarioRepository> _repositoryMock;
    private readonly Mock<INotificadorService> _notificadorMock;
    private readonly Mock<ILogger<UsuarioService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly UsuarioService _sut;

    public UsuarioServiceTests()
    {
        _repositoryMock   = new Mock<IUsuarioRepository>();
        _notificadorMock  = new Mock<INotificadorService>();
        _loggerMock       = new Mock<ILogger<UsuarioService>>();
        _configurationMock = new Mock<IConfiguration>();

        _sut = new UsuarioService(
            _loggerMock.Object,
            _configurationMock.Object,
            _notificadorMock.Object,
            _repositoryMock.Object);
    }

    #region AdicionarAsync

    [Fact]
    public async Task AdicionarAsnyc_QuandoEmailNaoExiste_DeveRetornarUsuarioCriado()
    {
        // Arrange
        var userName = "joao.silva";
        var email    = "joao@email.com";
        var tipo     = TipoUsuario.Cliente;

        var usuarioEsperado = new Usuario
        {
            UserName    = userName,
            Email       = email,
            Tipo        = tipo,
            Ativo       = true,
            DataCriacao = DateTime.Now
        };

        _repositoryMock
            .Setup(r => r.EmailExisteAsync(email))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Usuario>()))
            .ReturnsAsync(usuarioEsperado);

        // Act
        var resultado = await _sut.AdicionarAsnyc(userName, email, tipo);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(email, resultado.Email);
        Assert.Equal(userName, resultado.UserName);
        Assert.True(resultado.Ativo);
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Usuario>()), Times.Once);
    }

    [Fact]
    public async Task AdicionarAsnyc_QuandoEmailJaExiste_DeveLancarInvalidOperationException()
    {
        // Arrange
        var email = "duplicado@email.com";

        _repositoryMock
            .Setup(r => r.EmailExisteAsync(email))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AdicionarAsnyc("usuario", email, TipoUsuario.Funcionario));

        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Usuario>()), Times.Never);
    }

    #endregion

    #region AtualizarAsync

    [Fact]
    public async Task AtualizarAsync_QuandoUsuarioExisteEEmailDisponivel_DeveRetornarUsuarioAtualizado()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var usuario = new Usuario
        {
            Id       = id,
            UserName = "novo.nome",
            Email    = "novo@email.com",
            Tipo     = TipoUsuario.Administrador,
            Ativo    = true
        };

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(id))
            .ReturnsAsync(usuario);

        _repositoryMock
            .Setup(r => r.ObterPorEmailAsync(usuario.Email))
            .ReturnsAsync((Usuario?)null);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<Usuario>()))
            .ReturnsAsync(usuario);

        // Act
        var resultado = await _sut.AtualizarAsync(usuario);

        // Assert
        Assert.NotNull(resultado);
        Assert.NotNull(usuario.DataModificacao);
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Usuario>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoUsuarioNaoEncontrado_DeveLancarKeyNotFoundException()
    {
        // Arrange
        var usuario = new Usuario { Id = Guid.NewGuid().ToString(), Email = "x@email.com" };

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(usuario.Id))
            .ReturnsAsync((Usuario?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.AtualizarAsync(usuario));

        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoEmailEmUsoDeOutroUsuario_DeveLancarInvalidOperationException()
    {
        // Arrange
        var id          = Guid.NewGuid().ToString();
        var outroId     = Guid.NewGuid().ToString();
        var emailEmUso  = "emuso@email.com";

        var usuario     = new Usuario { Id = id, Email = emailEmUso };
        var outroUsuario = new Usuario { Id = outroId, Email = emailEmUso };

        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(usuario);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(emailEmUso)).ReturnsAsync(outroUsuario);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AtualizarAsync(usuario));
    }

    #endregion

    #region ObterTodosAsync

    [Fact]
    public async Task ObterTodosAsync_DeveRetornarListaDeUsuarios()
    {
        // Arrange
        var lista = new List<Usuario>
        {
            new() { Id = Guid.NewGuid().ToString(), UserName = "user1", Email = "u1@email.com" },
            new() { Id = Guid.NewGuid().ToString(), UserName = "user2", Email = "u2@email.com" }
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

    #region RemoverAsync

    [Fact]
    public async Task RemoverAsync_QuandoUsuarioExiste_DeveChamarRepositorio()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var usuario = new Usuario { Id = id };

        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(usuario);
        _repositoryMock.Setup(r => r.RemoverAsync(id)).Returns(Task.CompletedTask);

        // Act
        await _sut.RemoverAsync(id);

        // Assert
        _repositoryMock.Verify(r => r.RemoverAsync(id), Times.Once);
    }

    [Fact]
    public async Task RemoverAsync_QuandoUsuarioNaoExiste_DeveLancarKeyNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();

        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync((Usuario?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.RemoverAsync(id));

        _repositoryMock.Verify(r => r.RemoverAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion
}