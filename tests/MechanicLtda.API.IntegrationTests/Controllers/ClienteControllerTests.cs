using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MechanicLtda.API.IntegrationTests.Helpers;
using Xunit;

namespace MechanicLtda.API.IntegrationTests.Controllers;

public class ClienteControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ClienteControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─── helpers ────────────────────────────────────────────────────────────────

    private async Task AutenticarAsync()
    {
        // Cadastra e autentica um administrador para os testes
        var token = await AuthHelper.ObterTokenAsync(_client, "admin@mechanic.com", "Admin@123");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    // ─── GET /api/cliente ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetClientes_SemToken_DeveRetornar401()
    {
        // Act
        var response = await _client.GetAsync("/api/cliente");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetClientes_ComToken_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.GetAsync("/api/cliente");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─── POST /api/cliente ───────────────────────────────────────────────────────

    [Fact]
    public async Task PostCliente_DadosValidos_DeveRetornar201()
    {
        // Arrange
        await AutenticarAsync();

        var payload = new
        {
            nome     = "João Silva",
            email    = "joao@teste.com",
            telefone = "11999999999",
            cpfCnpj  = "52998224725"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/cliente", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostCliente_EmailDuplicado_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        var payload = new
        {
            nome     = "Maria Souza",
            email    = "duplicado@teste.com",
            cpfCnpj  = "52998224725"
        };

        await _client.PostAsJsonAsync("/api/cliente", payload);

        // Act — segundo cadastro com mesmo e-mail
        var response = await _client.PostAsJsonAsync("/api/cliente", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── GET /api/cliente/{id} ───────────────────────────────────────────────────

    [Fact]
    public async Task GetClientePorId_IdInexistente_DeveRetornar404()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.GetAsync("/api/cliente/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}