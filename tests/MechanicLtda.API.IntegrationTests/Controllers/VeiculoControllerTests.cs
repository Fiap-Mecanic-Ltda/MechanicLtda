using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MechanicLtda.API.IntegrationTests.Helpers;
using Xunit;

namespace MechanicLtda.API.IntegrationTests.Controllers;

public class VeiculoControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public VeiculoControllerTests(CustomWebApplicationFactory factory)
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

    private static string GerarPlacaUnica()
    {
        var b  = Guid.NewGuid().ToByteArray();
        var l1 = (char)('A' + b[0] % 26);
        var l2 = (char)('A' + b[1] % 26);
        var l3 = (char)('A' + b[2] % 26);
        var n  = (b[3] * 256 + b[4]) % 10000;
        return $"{l1}{l2}{l3}{n:D4}";
    }

    private async Task<int> CriarClienteEObterIdAsync()
    {
        var payload = new
        {
            nome     = "Cliente Veículo",
            email    = $"cliente.veiculo.{Guid.NewGuid():N}@teste.com",
            cpfCnpj  = "52998224725",
            telefone = "11999999999"
        };

        var response = await _client.PostAsJsonAsync("/api/cliente", payload);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<IdData>();
        return body!.Id;
    }

    private async Task<(int Id, string Placa)> CriarVeiculoEObterAsync(int clienteId)
    {
        var placa   = GerarPlacaUnica();
        var payload = new
        {
            placa     = placa,
            marca     = "Toyota",
            modelo    = "Corolla",
            ano       = 2022,
            clienteId = clienteId
        };

        var response = await _client.PostAsJsonAsync("/api/veiculo", payload);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<IdData>();
        return (body!.Id, placa);
    }

    // ─── GET /api/veiculo ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetVeiculos_SemToken_DeveRetornar401()
    {
        // Act
        var response = await _client.GetAsync("/api/veiculo");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetVeiculos_ComToken_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.GetAsync("/api/veiculo");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─── POST /api/veiculo ───────────────────────────────────────────────────────

    [Fact]
    public async Task PostVeiculo_SemToken_DeveRetornar401()
    {
        // Arrange
        var payload = new
        {
            placa     = GerarPlacaUnica(),
            marca     = "Honda",
            modelo    = "Civic",
            ano       = 2023,
            clienteId = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/veiculo", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostVeiculo_DadosValidos_DeveRetornar201()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();

        var payload = new
        {
            placa     = GerarPlacaUnica(),
            marca     = "Fiat",
            modelo    = "Uno",
            ano       = 2020,
            clienteId = clienteId
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/veiculo", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostVeiculo_PlacaDuplicada_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var placa     = GerarPlacaUnica();

        var payload = new
        {
            placa     = placa,
            marca     = "VW",
            modelo    = "Gol",
            ano       = 2021,
            clienteId = clienteId
        };

        await _client.PostAsJsonAsync("/api/veiculo", payload);

        // Act — segundo cadastro com mesma placa
        var response = await _client.PostAsJsonAsync("/api/veiculo", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostVeiculo_ClienteInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        var payload = new
        {
            placa     = GerarPlacaUnica(),
            marca     = "Ford",
            modelo    = "Ka",
            ano       = 2019,
            clienteId = 99999
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/veiculo", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── GET /api/veiculo/{id} ───────────────────────────────────────────────────

    [Fact]
    public async Task GetVeiculoPorId_SemToken_DeveRetornar401()
    {
        // Act
        var response = await _client.GetAsync("/api/veiculo/1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetVeiculoPorId_IdExistente_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId       = await CriarClienteEObterIdAsync();
        var (veiculoId, _)  = await CriarVeiculoEObterAsync(clienteId);

        // Act
        var response = await _client.GetAsync($"/api/veiculo/{veiculoId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetVeiculoPorId_IdInexistente_DeveRetornar404()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.GetAsync("/api/veiculo/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─── GET /api/veiculo/cliente/{clienteId} ────────────────────────────────────

    [Fact]
    public async Task GetVeiculosPorCliente_SemToken_DeveRetornar401()
    {
        // Act
        var response = await _client.GetAsync("/api/veiculo/cliente/1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetVeiculosPorCliente_ClienteComVeiculos_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        await CriarVeiculoEObterAsync(clienteId);

        // Act
        var response = await _client.GetAsync($"/api/veiculo/cliente/{clienteId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetVeiculosPorCliente_ClienteSemVeiculos_DeveRetornar200ComListaVazia()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();

        // Act
        var response = await _client.GetAsync($"/api/veiculo/cliente/{clienteId}");
        var body     = await response.Content.ReadFromJsonAsync<IEnumerable<VeiculoData>>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Empty(body);
    }

    // ─── PUT /api/veiculo/{id} ───────────────────────────────────────────────────

    [Fact]
    public async Task PutVeiculo_SemToken_DeveRetornar401()
    {
        // Act
        var response = await _client.PutAsJsonAsync(
            "/api/veiculo/1",
            new { placa = "AAA1111", marca = "Fiat", modelo = "Uno", ano = 2020, ativo = true, clienteId = 1 });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutVeiculo_DadosValidos_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId      = await CriarClienteEObterIdAsync();
        var (veiculoId, _) = await CriarVeiculoEObterAsync(clienteId);

        var payload = new
        {
            placa     = GerarPlacaUnica(),
            marca     = "Honda",
            modelo    = "Fit",
            ano       = 2023,
            ativo     = true,
            clienteId = clienteId
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/veiculo/{veiculoId}", payload);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PutVeiculo_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();

        var payload = new
        {
            placa     = GerarPlacaUnica(),
            marca     = "VW",
            modelo    = "Polo",
            ano       = 2022,
            ativo     = true,
            clienteId = clienteId
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/veiculo/99999", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutVeiculo_PlacaJaEmUso_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId       = await CriarClienteEObterIdAsync();
        var (veiculo1Id, _) = await CriarVeiculoEObterAsync(clienteId);
        var (_, placa2)     = await CriarVeiculoEObterAsync(clienteId);

        // Tenta atualizar o veículo 1 com a placa do veículo 2
        var payload = new
        {
            placa     = placa2,
            marca     = "Toyota",
            modelo    = "Corolla",
            ano       = 2022,
            ativo     = true,
            clienteId = clienteId
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/veiculo/{veiculo1Id}", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── DELETE /api/veiculo/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task DeleteVeiculo_SemToken_DeveRetornar401()
    {
        // Act
        var response = await _client.DeleteAsync("/api/veiculo/1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteVeiculo_IdExistente_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId      = await CriarClienteEObterIdAsync();
        var (veiculoId, _) = await CriarVeiculoEObterAsync(clienteId);

        // Act
        var response = await _client.DeleteAsync($"/api/veiculo/{veiculoId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteVeiculo_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.DeleteAsync("/api/veiculo/99999");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteVeiculo_AposDeletar_GetDeveRetornar404()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId      = await CriarClienteEObterIdAsync();
        var (veiculoId, _) = await CriarVeiculoEObterAsync(clienteId);

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/veiculo/{veiculoId}");
        deleteResponse.EnsureSuccessStatusCode();

        var getResponse = await _client.GetAsync($"/api/veiculo/{veiculoId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    // ─── helpers de desserialização ─────────────────────────────────────────────

    private sealed class IdData
    {
        public int Id { get; set; }
    }

    private sealed class VeiculoData
    {
        public int    Id        { get; set; }
        public string Placa     { get; set; } = null!;
        public string Marca     { get; set; } = null!;
        public string Modelo    { get; set; } = null!;
        public int    Ano       { get; set; }
        public bool   Ativo     { get; set; }
        public int    ClienteId { get; set; }
    }
}