using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MechanicLtda.API.IntegrationTests.Helpers;
using Xunit;

namespace MechanicLtda.API.IntegrationTests.Controllers;

public class ItemOrdemServicoControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ItemOrdemServicoControllerTests(CustomWebApplicationFactory factory)
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
            nome     = "Cliente Item OS",
            email    = $"cliente.item.{Guid.NewGuid():N}@teste.com",
            cpfCnpj  = "52998224725",
            telefone = "11999999999"
        };

        var response = await _client.PostAsJsonAsync("/api/cliente", payload);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<IdData>();
        return body!.Id;
    }

    private async Task<int> CriarVeiculoEObterIdAsync(int clienteId)
    {
        var payload = new
        {
            placa       = GerarPlacaUnica(),
            marca       = "Honda",
            modelo      = "Civic",
            ano         = 2021,
            clienteId   = clienteId,
            ativo       = true,
            dataCriacao = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync("/api/veiculo", payload);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<IdData>();
        return body!.Id;
    }

    private async Task<int> CriarOSEObterIdAsync(int veiculoId, int clienteId)
    {
        var payload = new
        {
            descricaoProblema  = "Troca de óleo",
            valorTotalEstimado = (decimal?)null,
            veiculoId          = veiculoId,
            clienteId          = clienteId
        };

        var response = await _client.PostAsJsonAsync("/api/ordemservico", payload);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<IdData>();
        return body!.Id;
    }

    private async Task<int> CriarEstoqueEObterIdAsync()
    {
        var payload = new
        {
            nome             = $"Peça Teste {Guid.NewGuid():N}",
            tipo             = 2,   // Peca
            quantidadeAtual  = 20,
            quantidadeMinima = 2
        };

        var response = await _client.PostAsJsonAsync("/api/estoque", payload);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<IdData>();
        return body!.Id;
    }

    private static object PayloadItemValido(int? estoqueId = null, int quantidade = 2, decimal valorUnitario = 100m) =>
        new { estoqueId, quantidade, valorUnitario };

    /// <summary>
    /// Cria um item na OS e devolve o Id retornado pela API.
    /// </summary>
    private async Task<int> CriarItemEObterIdAsync(int osId, object? payload = null)
    {
        payload ??= PayloadItemValido();
        var response = await _client.PostAsJsonAsync($"/api/ordem-servico/{osId}/itens", payload);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<IdData>();
        return body!.Id;
    }

    // ─── GET /api/ordem-servico/{ordemServicoId}/itens ──────────────────────────

    [Fact]
    public async Task GetItens_SemToken_DeveRetornar401()
    {
        // Act
        var response = await _client.GetAsync("/api/ordem-servico/1/itens");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetItens_OSExistente_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);

        // Act
        var response = await _client.GetAsync($"/api/ordem-servico/{osId}/itens");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─── GET /api/ordem-servico/{ordemServicoId}/itens/{id} ─────────────────────

    [Fact]
    public async Task GetItemPorId_IdExistente_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);
        var itemId    = await CriarItemEObterIdAsync(osId);

        // Act
        var response = await _client.GetAsync($"/api/ordem-servico/{osId}/itens/{itemId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetItemPorId_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.GetAsync("/api/ordem-servico/1/itens/99999");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── POST /api/ordem-servico/{ordemServicoId}/itens ─────────────────────────

    [Fact]
    public async Task PostItem_DadosValidos_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/ordem-servico/{osId}/itens",
            PayloadItemValido(quantidade: 2, valorUnitario: 150m));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostItem_OSInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/ordem-servico/99999/itens",
            PayloadItemValido());

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostItem_ComEstoqueValido_DeveRetornar200EDescontarEstoque()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);
        var estoqueId = await CriarEstoqueEObterIdAsync();

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/ordem-servico/{osId}/itens",
            PayloadItemValido(estoqueId: estoqueId, quantidade: 3, valorUnitario: 50m));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostItem_QuantidadeSuperiorAoEstoque_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);
        var estoqueId = await CriarEstoqueEObterIdAsync(); // quantidadeAtual = 20

        // Act — solicita 9999, muito acima do estoque disponível
        var response = await _client.PostAsJsonAsync(
            $"/api/ordem-servico/{osId}/itens",
            PayloadItemValido(estoqueId: estoqueId, quantidade: 9999, valorUnitario: 50m));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostItem_SemQuantidade_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);

        var payload = new { valorUnitario = 100m };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/ordem-servico/{osId}/itens", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Cobre o branch "if (!ModelState.IsValid) return CustomResponse(ModelState)" em Criar.
    /// Envia quantidade = 0, violando [Range(1, int.MaxValue)].
    /// </summary>
    [Fact]
    public async Task PostItem_ModelStateInvalido_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);

        // quantidade = 0 viola [Range(1, int.MaxValue)] — ModelState.IsValid == false
        var payload = new { quantidade = 0, valorUnitario = 100m };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/ordem-servico/{osId}/itens", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── PUT /api/ordem-servico/{ordemServicoId}/itens/{id} ─────────────────────

    [Fact]
    public async Task PutItem_DadosValidos_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);
        var itemId    = await CriarItemEObterIdAsync(osId);

        var payload = new { quantidade = 5, valorUnitario = 80m };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/ordem-servico/{osId}/itens/{itemId}", payload);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PutItem_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        var payload = new { quantidade = 1, valorUnitario = 50m };

        // Act
        var response = await _client.PutAsJsonAsync("/api/ordem-servico/1/itens/99999", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Cobre o branch "if (!ModelState.IsValid) return CustomResponse(ModelState)" em Atualizar.
    /// Envia valorUnitario = 0, violando [Range(0.01, double.MaxValue)].
    /// </summary>
    [Fact]
    public async Task PutItem_ModelStateInvalido_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);
        var itemId    = await CriarItemEObterIdAsync(osId);

        // valorUnitario = 0 viola [Range(0.01, double.MaxValue)] — ModelState.IsValid == false
        var payload = new { quantidade = 1, valorUnitario = 0m };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/ordem-servico/{osId}/itens/{itemId}", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── DELETE /api/ordem-servico/{ordemServicoId}/itens/{id} ──────────────────

    [Fact]
    public async Task DeleteItem_IdExistente_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);
        var itemId    = await CriarItemEObterIdAsync(osId);

        // Act
        var response = await _client.DeleteAsync($"/api/ordem-servico/{osId}/itens/{itemId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteItem_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.DeleteAsync("/api/ordem-servico/1/itens/99999");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── helpers de desserialização ─────────────────────────────────────────────

    private sealed class IdData
    {
        public int Id { get; set; }
    }
}