using AutoMapper;
using MechanicLtda.Application.AppServices;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;
using MechanicLtda.Domain.Interfaces.Services;
using Moq;
using Xunit;

namespace MechanicLtda.Application.Tests;

public class UsuarioAppServiceTests
{
    private readonly Mock<IUsuarioService> _serviceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly UsuarioAppService _sut;

    public UsuarioAppServiceTests()
    {
        _serviceMock = new Mock<IUsuarioService>();
        _mapperMock  = new Mock<IMapper>();
        _sut         = new UsuarioAppService(_serviceMock.Object, _mapperMock.Object);
    }

    #region AdicionarAsync

    [Fact]
    public async Task AdicionarAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var dto = new UsuarioCreateDto
        {
            UserName = "maria",
            Email    = "maria@email.com",
            Tipo     = TipoUsuario.Funcionario
        };

        var usuarioCriado = new Usuario { UserName = dto.UserName, Email = dto.Email };
        var usuarioDto    = new UsuarioDto  { UserName = dto.UserName, Email = dto.Email };

        _serviceMock
            .Setup(s => s.AdicionarAsnyc(dto.UserName, dto.Email, dto.Tipo))
            .ReturnsAsync(usuarioCriado);

        _mapperMock
            .Setup(m => m.Map<UsuarioDto>(usuarioCriado))
            .Returns(usuarioDto);

        // Act
        var response = await _sut.AdicionarAsync(dto);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(usuarioDto, response.getResponse);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoServicoLancaExcecao_DeveRetornarResponseComErro()
    {
        // Arrange
        var dto = new UsuarioCreateDto { UserName = "fail", Email = "fail@email.com" };

        _serviceMock
            .Setup(s => s.AdicionarAsnyc(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TipoUsuario>()))
            .ThrowsAsync(new InvalidOperationException("[show]E-mail já cadastrado."));

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
        var id  = Guid.NewGuid().ToString();
        var dto = new UsuarioUpdateDto { UserName = "atualizado", Email = "atualizado@email.com", Ativo = true };

        var entidade   = new Usuario { Id = id, UserName = dto.UserName, Email = dto.Email };
        var usuarioDto = new UsuarioDto { UserName = dto.UserName, Email = dto.Email };

        _mapperMock.Setup(m => m.Map<Usuario>(dto)).Returns(entidade);
        _serviceMock.Setup(s => s.AtualizarAsync(It.IsAny<Usuario>())).ReturnsAsync(entidade);
        _mapperMock.Setup(m => m.Map<UsuarioDto>(entidade)).Returns(usuarioDto);

        // Act
        var response = await _sut.AtualizarAsync(id, dto);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(usuarioDto, response.getResponse);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoUsuarioNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        var id  = Guid.NewGuid().ToString();
        var dto = new UsuarioUpdateDto { UserName = "x", Email = "x@email.com" };

        _mapperMock.Setup(m => m.Map<Usuario>(dto)).Returns(new Usuario());
        _serviceMock
            .Setup(s => s.AtualizarAsync(It.IsAny<Usuario>()))
            .ThrowsAsync(new KeyNotFoundException("Usuário não encontrado."));

        // Act
        var response = await _sut.AtualizarAsync(id, dto);

        // Assert
        Assert.True(response.hasErrors);
    }

    #endregion

    #region ObterTodosAsync

    [Fact]
    public async Task ObterTodosAsync_QuandoExistemUsuarios_DeveRetornarListaMapeada()
    {
        // Arrange
        var usuarios = new List<Usuario>
        {
            new() { UserName = "u1", Email = "u1@email.com" },
            new() { UserName = "u2", Email = "u2@email.com" }
        };

        var dtos = usuarios.Select(u => new UsuarioDto { UserName = u.UserName, Email = u.Email });

        _serviceMock.Setup(s => s.ObterTodosAsync()).ReturnsAsync(usuarios);
        _mapperMock.Setup(m => m.Map<IEnumerable<UsuarioDto>>(usuarios)).Returns(dtos);

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

    #region RemoverAsync

    [Fact]
    public async Task RemoverAsync_QuandoSucesso_DeveRetornarTrue()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _serviceMock.Setup(s => s.RemoverAsync(id)).Returns(Task.CompletedTask);

        // Act
        var response = await _sut.RemoverAsync(id);

        // Assert
        Assert.False(response.hasErrors);
        Assert.True(response.getResponse);
    }

    [Fact]
    public async Task RemoverAsync_QuandoUsuarioNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _serviceMock
            .Setup(s => s.RemoverAsync(id))
            .ThrowsAsync(new KeyNotFoundException("Usuário não encontrado."));

        // Act
        var response = await _sut.RemoverAsync(id);

        // Assert
        Assert.True(response.hasErrors);
        Assert.False(response.getResponse);
    }

    #endregion
}