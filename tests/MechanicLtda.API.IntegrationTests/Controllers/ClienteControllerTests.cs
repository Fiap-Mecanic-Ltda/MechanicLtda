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
        var token = await AuthHelper.ObterTokenAsync(_client, "admin@mechanic.com", "Admin@123");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<int> CriarClienteEObterIdAsync(
        string? email = null,
        string cpfCnpj = "52998224725")
    {
        var payload = new
        {
            nome     = "Cliente Teste",
            email    = email ?? $"cliente.{Guid.NewGuid():N}@teste.com",
            cpfCnpj  = cpfCnpj,
            telefone = "11999999999"
        };

        var response = await _client.PostAsJsonAsync("/api/cliente", payload);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<IdData>();
        return body!.Id;
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

    /// <summary>
    /// Cobre o branch "if (result.hasErrors) return NotFound" → false em ObterPorId,
    /// ou seja, o caminho feliz onde o cliente existe.
    /// </summary>
    [Fact]
    public async Task GetClientePorId_IdExistente_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();

        // Act
        var response = await _client.GetAsync($"/api/cliente/{clienteId}");

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
            email    = $"joao.{Guid.NewGuid():N}@teste.com",
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

        var emailFixo = $"dup.{Guid.NewGuid():N}@teste.com";
        var payload = new
        {
            nome     = "Maria Souza",
            email    = emailFixo,
            cpfCnpj  = "52998224725"
        };

        await _client.PostAsJsonAsync("/api/cliente", payload);

        // Act — segundo cadastro com mesmo e-mail
        var response = await _client.PostAsJsonAsync("/api/cliente", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Cobre o branch "if (!ModelState.IsValid) return CustomResponse(ModelState)" em Criar.
    /// Nome ausente viola [Required].
    /// </summary>
    [Fact]
    public async Task PostCliente_ModelStateInvalido_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        // nome ausente — viola [Required]
        var payload = new
        {
            email   = "sem.nome@teste.com",
            cpfCnpj = "52998224725"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/cliente", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── PUT /api/cliente/{id} ───────────────────────────────────────────────────

    /// <summary>
    /// Cobre o caminho feliz de Atualizar (sem erros de modelo ou de negócio).
    /// </summary>
    [Fact]
    public async Task PutCliente_DadosValidos_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();

        var payload = new
        {
            nome     = "Nome Atualizado",
            email    = $"atualizado.{Guid.NewGuid():N}@teste.com",
            cpfCnpj  = "52998224725",
            telefone = "11988887777",
            ativo    = true
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/cliente/{clienteId}", payload);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Cobre o branch "if (!ModelState.IsValid) return CustomResponse(ModelState)" em Atualizar.
    /// E-mail inválido viola [EmailAddress].
    /// </summary>
    [Fact]
    public async Task PutCliente_ModelStateInvalido_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();

        // email inválido viola [EmailAddress]
        var payload = new
        {
            nome     = "Nome",
            email    = "nao-e-um-email",
            cpfCnpj  = "52998224725",
            telefone = "11988887777",
            ativo    = true
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/cliente/{clienteId}", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Cobre o branch de erro de negócio em Atualizar (id inexistente → hasErrors = true).
    /// </summary>
    [Fact]
    public async Task PutCliente_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        var payload = new
        {
            nome     = "Nome",
            email    = $"inexistente.{Guid.NewGuid():N}@teste.com",
            cpfCnpj  = "52998224725",
            telefone = "11988887777",
            ativo    = true
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/cliente/99999", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── DELETE /api/cliente/{id} ────────────────────────────────────────────────

    /// <summary>
    /// Cobre o caminho feliz de Remover.
    /// </summary>
    [Fact]
    public async Task DeleteCliente_IdExistente_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/cliente/{clienteId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Cobre o branch de erro de negócio em Remover (id inexistente → hasErrors = true).
    /// </summary>
    [Fact]
    public async Task DeleteCliente_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.DeleteAsync("/api/cliente/99999");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── helpers de desserialização ─────────────────────────────────────────────

    private sealed class IdData
    {
        public int Id { get; set; }
    }
}