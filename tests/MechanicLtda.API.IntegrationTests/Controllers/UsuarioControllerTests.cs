using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MechanicLtda.API.IntegrationTests.Helpers;
using Xunit;

namespace MechanicLtda.API.IntegrationTests.Controllers;

public class UsuarioControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsuarioControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─── helpers ────────────────────────────────────────────────────────────────

    private async Task AutenticarAsync()
    {
        var token = await AuthHelper.ObterTokenAsync(
            _client,
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminSenha);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private static object PayloadValido(string sufixo = "01") => new
    {
        userName = $"usuario.teste{sufixo}",
        email    = $"usuario{sufixo}@teste.com",
        senha    = "Teste@123",
        tipo     = 2   // Funcionario
    };

    // ─── GET /api/usuario ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsuarios_SemToken_DeveRetornar401()
    {
        // Act
        var response = await _client.GetAsync("/api/usuario");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUsuarios_ComTokenAdministrador_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.GetAsync("/api/usuario");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─── POST /api/usuario ───────────────────────────────────────────────────────

    [Fact]
    public async Task PostUsuario_SemToken_DeveRetornar401()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/usuario", PayloadValido());

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostUsuario_DadosValidos_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.PostAsJsonAsync("/api/usuario", PayloadValido("02"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostUsuario_EmailDuplicado_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        var payload = PayloadValido("03");

        await _client.PostAsJsonAsync("/api/usuario", payload);

        // Act — segundo cadastro com mesmo e-mail
        var response = await _client.PostAsJsonAsync("/api/usuario", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── PUT /api/usuario/{id} ───────────────────────────────────────────────────

    [Fact]
    public async Task PutUsuario_SemToken_DeveRetornar401()
    {
        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/usuario/{Guid.NewGuid()}",
            new { userName = "qualquer", email = "qualquer@teste.com", tipo = 2, ativo = true });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutUsuario_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        var payload = new
        {
            userName = "usuario.alterado",
            email    = "alterado@teste.com",
            tipo     = 2,
            ativo    = true
        };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/usuario/{Guid.NewGuid()}",
            payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutUsuario_IdValido_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();

        // Cria o usuário para obter o Id
        var criarResponse = await _client.PostAsJsonAsync("/api/usuario", PayloadValido("04"));
        criarResponse.EnsureSuccessStatusCode();

        var json      = await criarResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var id        = doc.RootElement.GetProperty("id").GetString()!;

        var payload = new
        {
            userName = "usuario.atualizado",
            email    = "atualizado04@teste.com",
            tipo     = 2,
            ativo    = true
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/usuario/{id}", payload);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─── DELETE /api/usuario/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task DeleteUsuario_SemToken_DeveRetornar401()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/usuario/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUsuario_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/usuario/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUsuario_IdValido_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();

        // Cria o usuário para obter o Id
        var criarResponse = await _client.PostAsJsonAsync("/api/usuario", PayloadValido("05"));
        criarResponse.EnsureSuccessStatusCode();

        var json      = await criarResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var id        = doc.RootElement.GetProperty("id").GetString()!;

        // Act
        var response = await _client.DeleteAsync($"/api/usuario/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}