using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MechanicLtda.API.IntegrationTests.Helpers;
using Xunit;

namespace MechanicLtda.API.IntegrationTests.Controllers;

public class EstoqueControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public EstoqueControllerTests(CustomWebApplicationFactory factory)
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

    private static object PayloadEstoqueValido(
        string nome      = "Filtro de Óleo",
        int    tipo      = 2,   // 2 = Peca
        int    qtdAtual  = 10,
        int    qtdMinima = 2) =>
        new { nome, tipo, quantidadeAtual = qtdAtual, quantidadeMinima = qtdMinima };

    /// <summary>
    /// Cria um estoque via POST e devolve o Id retornado pela API.
    /// </summary>
    private async Task<int> CriarEstoqueEObterIdAsync(object? payload = null)
    {
        payload ??= PayloadEstoqueValido();
        var response = await _client.PostAsJsonAsync("/api/estoque", payload);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<EstoqueData>();
        return body!.Id;
    }

    // ─── GET /api/estoque ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetEstoque_SemToken_DeveRetornar401()
    {
        // Act
        var response = await _client.GetAsync("/api/estoque");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetEstoque_ComToken_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.GetAsync("/api/estoque");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─── GET /api/estoque/tipo/{tipo} ────────────────────────────────────────────

    [Fact]
    public async Task GetEstoquePorTipo_TipoPeca_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.GetAsync("/api/estoque/tipo/2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEstoquePorTipo_TipoInsumo_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.GetAsync("/api/estoque/tipo/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─── GET /api/estoque/{id} ───────────────────────────────────────────────────

    [Fact]
    public async Task GetEstoquePorId_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.GetAsync("/api/estoque/99999");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEstoquePorId_IdExistente_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var id = await CriarEstoqueEObterIdAsync();

        // Act
        var response = await _client.GetAsync($"/api/estoque/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─── POST /api/estoque ───────────────────────────────────────────────────────

    [Fact]
    public async Task PostEstoque_DadosValidos_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.PostAsJsonAsync("/api/estoque", PayloadEstoqueValido("Pastilha de Freio"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostEstoque_SemNome_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        var payload = new { tipo = 2, quantidadeAtual = 5, quantidadeMinima = 1 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/estoque", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── PUT /api/estoque/{id} ───────────────────────────────────────────────────

    [Fact]
    public async Task PutEstoque_DadosValidos_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var id = await CriarEstoqueEObterIdAsync();

        var payload = new { nome = "Filtro de Ar Atualizado", tipo = 2, quantidadeMinima = 5 };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/estoque/{id}", payload);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PutEstoque_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        var payload = new { nome = "Inexistente", tipo = 2, quantidadeMinima = 1 };

        // Act
        var response = await _client.PutAsJsonAsync("/api/estoque/99999", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── DELETE /api/estoque/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task DeleteEstoque_IdExistente_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var id = await CriarEstoqueEObterIdAsync(PayloadEstoqueValido("Item para Deletar"));

        // Act
        var response = await _client.DeleteAsync($"/api/estoque/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEstoque_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.DeleteAsync("/api/estoque/99999");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── PATCH /api/estoque/{id}/reposicao ──────────────────────────────────────

    [Fact]
    public async Task PatchReposicao_QuantidadeValida_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var id = await CriarEstoqueEObterIdAsync();

        var payload = new { quantidadeEntrada = 20 };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/estoque/{id}/reposicao", payload);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PatchReposicao_QuantidadeZero_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();
        var id = await CriarEstoqueEObterIdAsync();

        var payload = new { quantidadeEntrada = 0 };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/estoque/{id}/reposicao", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchReposicao_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        var payload = new { quantidadeEntrada = 5 };

        // Act
        var response = await _client.PatchAsJsonAsync("/api/estoque/99999/reposicao", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── Autorização por role ────────────────────────────────────────────────────

    [Fact]
    public async Task GetEstoque_ComTokenDeCliente_DeveRetornar403()
    {
        // Arrange
        var token = await AuthHelper.ObterTokenAsync(
            _client, CustomWebApplicationFactory.ClienteEmail, CustomWebApplicationFactory.ClienteSenha);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act — [Authorize(Roles = Roles.Admin)] não inclui a role Cliente
        var response = await _client.GetAsync("/api/estoque");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ─── helpers de desserialização ─────────────────────────────────────────────

    private sealed class EstoqueData
    {
        public int Id { get; set; }
    }
}