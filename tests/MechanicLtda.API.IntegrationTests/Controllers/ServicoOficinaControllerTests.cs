using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MechanicLtda.API.IntegrationTests.Helpers;
using Xunit;

namespace MechanicLtda.API.IntegrationTests.Controllers;

public class ServicoOficinaControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ServicoOficinaControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─── helpers ────────────────────────────────────────────────────────────────

    private async Task AutenticarAsync()
    {
        var token = await AuthHelper.ObterTokenAsync(_client, "admin@mechanic.com", "Admin@123");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private static object PayloadServicoValido(
        string nome      = "Troca de Óleo",
        string descricao = "Troca de óleo e filtro",
        decimal valorBase = 150m,
        bool ativo        = true) =>
        new { nome, descricao, valorBase, ativo };

    /// <summary>
    /// Cria um serviço via POST e devolve o Id retornado pela API.
    /// </summary>
    private async Task<int> CriarServicoEObterIdAsync(object? payload = null)
    {
        payload ??= PayloadServicoValido();
        var response = await _client.PostAsJsonAsync("/api/servicos-oficina", payload);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ServicoOficinaData>();
        return body!.Id;
    }

    // ─── GET /api/servicos-oficina ───────────────────────────────────────────────

    [Fact]
    public async Task GetServicoOficina_SemToken_DeveRetornar401()
    {
        // Act
        var response = await _client.GetAsync("/api/servicos-oficina");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetServicoOficina_ComToken_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.GetAsync("/api/servicos-oficina");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─── GET /api/servicos-oficina/ativos ────────────────────────────────────────

    [Fact]
    public async Task GetServicoOficinaAtivos_ComToken_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.GetAsync("/api/servicos-oficina/ativos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─── GET /api/servicos-oficina/{id} ──────────────────────────────────────────

    [Fact]
    public async Task GetServicoOficinaPorId_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.GetAsync("/api/servicos-oficina/99999");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetServicoOficinaPorId_IdExistente_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var id = await CriarServicoEObterIdAsync();

        // Act
        var response = await _client.GetAsync($"/api/servicos-oficina/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─── POST /api/servicos-oficina ──────────────────────────────────────────────

    [Fact]
    public async Task PostServicoOficina_DadosValidos_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.PostAsJsonAsync("/api/servicos-oficina", PayloadServicoValido("Alinhamento"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostServicoOficina_SemNome_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        var payload = new { descricao = "Sem nome", valorBase = 100m };

        // Act
        var response = await _client.PostAsJsonAsync("/api/servicos-oficina", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostServicoOficina_ValorBaseZero_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        var payload = PayloadServicoValido(valorBase: 0m);

        // Act
        var response = await _client.PostAsJsonAsync("/api/servicos-oficina", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── PUT /api/servicos-oficina/{id} ──────────────────────────────────────────

    [Fact]
    public async Task PutServicoOficina_DadosValidos_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var id = await CriarServicoEObterIdAsync();

        var payload = PayloadServicoValido("Troca de Óleo Sintético", valorBase: 220m);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/servicos-oficina/{id}", payload);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PutServicoOficina_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        var payload = PayloadServicoValido();

        // Act
        var response = await _client.PutAsJsonAsync("/api/servicos-oficina/99999", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── DELETE /api/servicos-oficina/{id} ───────────────────────────────────────

    [Fact]
    public async Task DeleteServicoOficina_IdExistente_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var id = await CriarServicoEObterIdAsync(PayloadServicoValido("Serviço para Deletar"));

        // Act
        var response = await _client.DeleteAsync($"/api/servicos-oficina/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteServicoOficina_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.DeleteAsync("/api/servicos-oficina/99999");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── Autorização por role ────────────────────────────────────────────────────

    [Fact]
    public async Task GetServicoOficina_ComTokenDeCliente_DeveRetornar403()
    {
        // Arrange
        var token = await AuthHelper.ObterTokenAsync(
            _client, CustomWebApplicationFactory.ClienteEmail, CustomWebApplicationFactory.ClienteSenha);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act — [Authorize(Roles = Roles.Admin)] não inclui Cliente
        var response = await _client.GetAsync("/api/servicos-oficina");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ─── helpers de desserialização ─────────────────────────────────────────────

    private sealed class ServicoOficinaData
    {
        public int Id { get; set; }
    }
}
