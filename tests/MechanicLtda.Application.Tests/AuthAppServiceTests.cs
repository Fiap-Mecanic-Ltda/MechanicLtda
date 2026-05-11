using MechanicLtda.Application.AppServices;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace MechanicLtda.Application.Tests;

/// <summary>
/// Substituto controlável para SignInManager, necessário pois o Moq não consegue
/// criar proxies de SignInManager quando o UserManager também é um proxy (Castle DynamicProxy).
/// </summary>
internal sealed class FakeSignInManager : SignInManager<Usuario>
{
    public SignInResult ResultadoCheckPassword { get; set; } = SignInResult.Success;

    public FakeSignInManager(UserManager<Usuario> userManager)
        : base(
            userManager,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<Usuario>>().Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<ILogger<SignInManager<Usuario>>>().Object,
            new Mock<IAuthenticationSchemeProvider>().Object,
            new Mock<IUserConfirmation<Usuario>>().Object)
    { }

    public override Task<SignInResult> CheckPasswordSignInAsync(
        Usuario user, string password, bool lockoutOnFailure)
        => Task.FromResult(ResultadoCheckPassword);
}

public class AuthAppServiceTests
{
    private readonly Mock<UserManager<Usuario>> _userManagerMock;
    private readonly FakeSignInManager         _fakeSignInManager;
    private readonly IConfiguration            _configuration;
    private readonly AuthAppService            _sut;

    public AuthAppServiceTests()
    {
        // Define a variável de ambiente necessária para GerarTokenAsync
        Environment.SetEnvironmentVariable("JWT_SECRET_KEY", "chave-secreta-de-teste-super-segura-12345678");

        var userStoreMock = new Mock<IUserStore<Usuario>>();

        _userManagerMock = new Mock<UserManager<Usuario>>(
            userStoreMock.Object,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<Usuario>>(),
            Array.Empty<IUserValidator<Usuario>>(),
            Array.Empty<IPasswordValidator<Usuario>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<Usuario>>>());

        _fakeSignInManager = new FakeSignInManager(_userManagerMock.Object);

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:ExpiracaoMinutos"] = "60",
                ["JwtSettings:Issuer"]           = "mechanic-tests",
                ["JwtSettings:Audience"]         = "mechanic-tests",
            })
            .Build();

        _sut = new AuthAppService(
            _userManagerMock.Object,
            _fakeSignInManager,
            _configuration);
    }

    #region LoginAsync

    [Fact]
    public async Task LoginAsync_QuandoCredenciaisValidas_DeveRetornarTokenSemErros()
    {
        // Arrange
        var email   = "admin@email.com";
        var senha   = "Senha@123";
        var usuario = new Usuario
        {
            Id       = Guid.NewGuid().ToString(),
            UserName = "admin",
            Email    = email,
            Tipo     = TipoUsuario.Administrador,
            Ativo    = true
        };

        _userManagerMock
            .Setup(u => u.FindByEmailAsync(email))
            .ReturnsAsync(usuario);

        _userManagerMock
            .Setup(u => u.GetRolesAsync(usuario))
            .ReturnsAsync(new List<string>());

        _fakeSignInManager.ResultadoCheckPassword = SignInResult.Success;

        // Act
        var response = await _sut.LoginAsync(email, senha);

        // Assert
        Assert.False(response.hasErrors);
        Assert.NotNull(response.getResponse);
        Assert.NotEmpty(response.getResponse.Token);
        Assert.True(response.getResponse.Expiracao > DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_QuandoUsuarioNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        _userManagerMock
            .Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((Usuario?)null);

        // Act
        var response = await _sut.LoginAsync("inexistente@email.com", "Senha@123");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    [Fact]
    public async Task LoginAsync_QuandoUsuarioInativo_DeveRetornarResponseComErro()
    {
        // Arrange
        var usuario = new Usuario
        {
            Id    = Guid.NewGuid().ToString(),
            Email = "inativo@email.com",
            Ativo = false
        };

        _userManagerMock
            .Setup(u => u.FindByEmailAsync(usuario.Email))
            .ReturnsAsync(usuario);

        // Act
        var response = await _sut.LoginAsync(usuario.Email, "Senha@123");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    [Fact]
    public async Task LoginAsync_QuandoSenhaIncorreta_DeveRetornarResponseComErro()
    {
        // Arrange
        var email   = "usuario@email.com";
        var usuario = new Usuario
        {
            Id    = Guid.NewGuid().ToString(),
            Email = email,
            Ativo = true
        };

        _userManagerMock
            .Setup(u => u.FindByEmailAsync(email))
            .ReturnsAsync(usuario);

        _fakeSignInManager.ResultadoCheckPassword = SignInResult.Failed;

        // Act
        var response = await _sut.LoginAsync(email, "SenhaErrada");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    [Fact]
    public async Task LoginAsync_QuandoExcecaoLancada_DeveRetornarResponseComErro()
    {
        // Arrange
        _userManagerMock
            .Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Erro inesperado"));

        // Act
        var response = await _sut.LoginAsync("erro@email.com", "Senha@123");

        // Assert
        Assert.True(response.hasErrors);
    }

    #endregion

    #region RegistrarAsync

    [Fact]
    public async Task RegistrarAsync_QuandoEmailDisponivel_DeveRetornarUsuarioDtoSemErros()
    {
        // Arrange
        var userName = "novo.usuario";
        var email    = "novo@email.com";
        var senha    = "Senha@123";
        var tipo     = TipoUsuario.Cliente;

        _userManagerMock
            .Setup(u => u.FindByEmailAsync(email))
            .ReturnsAsync((Usuario?)null);

        _userManagerMock
            .Setup(u => u.CreateAsync(It.IsAny<Usuario>(), senha))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var response = await _sut.RegistrarAsync(userName, email, senha, tipo);

        // Assert
        Assert.False(response.hasErrors);
        Assert.NotNull(response.getResponse);
        Assert.Equal(email, response.getResponse.Email);
        Assert.Equal(userName, response.getResponse.UserName);
        Assert.True(response.getResponse.Ativo);
    }

    [Fact]
    public async Task RegistrarAsync_QuandoEmailJaExiste_DeveRetornarResponseComErro()
    {
        // Arrange
        var email            = "existente@email.com";
        var usuarioExistente = new Usuario { Id = Guid.NewGuid().ToString(), Email = email };

        _userManagerMock
            .Setup(u => u.FindByEmailAsync(email))
            .ReturnsAsync(usuarioExistente);

        // Act
        var response = await _sut.RegistrarAsync("qualquer", email, "Senha@123", TipoUsuario.Funcionario);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
        _userManagerMock.Verify(u => u.CreateAsync(It.IsAny<Usuario>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarAsync_QuandoCreateFalha_DeveRetornarResponseComErrosDeIdentity()
    {
        // Arrange
        var email = "invalido@email.com";

        _userManagerMock
            .Setup(u => u.FindByEmailAsync(email))
            .ReturnsAsync((Usuario?)null);

        var erros = new[]
        {
            new IdentityError { Description = "A senha deve ter ao menos 8 caracteres." },
            new IdentityError { Description = "A senha deve conter um caractere especial." }
        };

        _userManagerMock
            .Setup(u => u.CreateAsync(It.IsAny<Usuario>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(erros));

        // Act
        var response = await _sut.RegistrarAsync("usuario", email, "fraca", TipoUsuario.Cliente);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Equal(2, response.getErrors.Count);
    }

    [Fact]
    public async Task RegistrarAsync_QuandoExcecaoLancada_DeveRetornarResponseComErro()
    {
        // Arrange
        _userManagerMock
            .Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Erro inesperado"));

        // Act
        var response = await _sut.RegistrarAsync("x", "x@email.com", "Senha@123", TipoUsuario.Cliente);

        // Assert
        Assert.True(response.hasErrors);
    }

    #endregion

    #region AlterarSenhaAsync

    [Fact]
    public async Task AlterarSenhaAsync_QuandoSucesso_DeveRetornarTrue()
    {
        // Arrange
        var email      = "usuario@email.com";
        var senhaAtual = "SenhaAtual@123";
        var novaSenha  = "NovaSenha@456";
        var usuario    = new Usuario { Id = Guid.NewGuid().ToString(), Email = email, Ativo = true };

        _userManagerMock
            .Setup(u => u.FindByEmailAsync(email))
            .ReturnsAsync(usuario);

        _userManagerMock
            .Setup(u => u.ChangePasswordAsync(usuario, senhaAtual, novaSenha))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var response = await _sut.AlterarSenhaAsync(email, senhaAtual, novaSenha);

        // Assert
        Assert.False(response.hasErrors);
        Assert.True(response.getResponse);
    }

    [Fact]
    public async Task AlterarSenhaAsync_QuandoUsuarioNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        _userManagerMock
            .Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((Usuario?)null);

        // Act
        var response = await _sut.AlterarSenhaAsync("inexistente@email.com", "Atual@123", "Nova@456");

        // Assert
        Assert.True(response.hasErrors);
        Assert.False(response.getResponse);
        _userManagerMock.Verify(
            u => u.ChangePasswordAsync(It.IsAny<Usuario>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task AlterarSenhaAsync_QuandoUsuarioInativo_DeveRetornarResponseComErro()
    {
        // Arrange
        var email   = "inativo@email.com";
        var usuario = new Usuario { Id = Guid.NewGuid().ToString(), Email = email, Ativo = false };

        _userManagerMock
            .Setup(u => u.FindByEmailAsync(email))
            .ReturnsAsync(usuario);

        // Act
        var response = await _sut.AlterarSenhaAsync(email, "Atual@123", "Nova@456");

        // Assert
        Assert.True(response.hasErrors);
        _userManagerMock.Verify(
            u => u.ChangePasswordAsync(It.IsAny<Usuario>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task AlterarSenhaAsync_QuandoSenhaAtualIncorreta_DeveRetornarResponseComErros()
    {
        // Arrange
        var email   = "usuario@email.com";
        var usuario = new Usuario { Id = Guid.NewGuid().ToString(), Email = email, Ativo = true };

        _userManagerMock
            .Setup(u => u.FindByEmailAsync(email))
            .ReturnsAsync(usuario);

        var erros = new[] { new IdentityError { Description = "Senha atual incorreta." } };

        _userManagerMock
            .Setup(u => u.ChangePasswordAsync(usuario, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(erros));

        // Act
        var response = await _sut.AlterarSenhaAsync(email, "SenhaErrada", "Nova@456");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Single(response.getErrors);
    }

    [Fact]
    public async Task AlterarSenhaAsync_QuandoExcecaoLancada_DeveRetornarResponseComErro()
    {
        // Arrange
        _userManagerMock
            .Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Erro inesperado"));

        // Act
        var response = await _sut.AlterarSenhaAsync("erro@email.com", "Atual@123", "Nova@456");

        // Assert
        Assert.True(response.hasErrors);
    }

    #endregion
}