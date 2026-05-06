using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MechanicLtda.API.IntegrationTests.Helpers;
using Xunit;

namespace MechanicLtda.API.IntegrationTests.Controllers;

public class OrcamentoControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrcamentoControllerTests(CustomWebApplicationFactory factory)
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
            nome     = "Cliente Orçamento",
            email    = $"cliente.orcamento.{Guid.NewGuid():N}@teste.com",
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
            marca       = "Toyota",
            modelo      = "Corolla",
            ano         = 2022,
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
            descricaoProblema  = "Revisão completa",
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
            nome             = $"Peça Orçamento {Guid.NewGuid():N}",
            tipo             = 2,   // Peca
            quantidadeAtual  = 20,
            quantidadeMinima = 2
        };

        var response = await _client.PostAsJsonAsync("/api/estoque", payload);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<IdData>();
        return body!.Id;
    }

    /// <summary>
    /// Cria um item na OS (o que aciona a geração automática do orçamento)
    /// e retorna o orçamento criado como <see cref="OrcamentoData"/>.
    /// </summary>
    private async Task<OrcamentoData> CriarOrcamentoViaItemEObterAsync(int osId, int? estoqueId = null)
    {
        var itemPayload  = new { estoqueId, quantidade = 2, valorUnitario = 150m };
        var itemResponse = await _client.PostAsJsonAsync($"/api/ordem-servico/{osId}/itens", itemPayload);
        itemResponse.EnsureSuccessStatusCode();

        var orcamentoResponse = await _client.GetAsync($"/api/orcamento/ordem-servico/{osId}");
        orcamentoResponse.EnsureSuccessStatusCode();

        // ✅ propriedade corrigida para "getResponse", que é o nome real no JSON
        var body = await orcamentoResponse.Content.ReadFromJsonAsync<OrcamentoResponseData>();
        return body!.GetResponse;
    }

    // ─── GET /api/orcamento/{id} ─────────────────────────────────────────────────

    [Fact]
    public async Task GetOrcamentoPorId_SemToken_DeveRetornar401()
    {
        // Act
        var response = await _client.GetAsync("/api/orcamento/1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrcamentoPorId_IdExistente_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);
        var orcamento = await CriarOrcamentoViaItemEObterAsync(osId);

        // Act
        var response = await _client.GetAsync($"/api/orcamento/{orcamento.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOrcamentoPorId_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.GetAsync("/api/orcamento/99999");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── GET /api/orcamento/ordem-servico/{ordemServicoId} ───────────────────────

    [Fact]
    public async Task GetOrcamentoPorOrdemServico_SemToken_DeveRetornar401()
    {
        // Act
        var response = await _client.GetAsync("/api/orcamento/ordem-servico/1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrcamentoPorOrdemServico_OSComOrcamento_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);
        await CriarOrcamentoViaItemEObterAsync(osId);

        // Act
        var response = await _client.GetAsync($"/api/orcamento/ordem-servico/{osId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOrcamentoPorOrdemServico_OSInexistente_DeveRetornar404()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.GetAsync("/api/orcamento/ordem-servico/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrcamentoPorOrdemServico_OSExistenteSemOrcamento_DeveRetornar404()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);

        // Act — OS existe mas sem itens, portanto sem orçamento
        var response = await _client.GetAsync($"/api/orcamento/ordem-servico/{osId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─── PUT /api/orcamento/{id} ─────────────────────────────────────────────────

    [Fact]
    public async Task PutOrcamento_SemToken_DeveRetornar401()
    {
        // Act
        var response = await _client.PutAsJsonAsync(
            "/api/orcamento/1",
            new { valorTotalPecas = 100m, valorTotalInsumos = 50m });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutOrcamento_DadosValidos_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);
        var orcamento = await CriarOrcamentoViaItemEObterAsync(osId);

        var payload = new { valorTotalPecas = 500m, valorTotalInsumos = 200m };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/orcamento/{orcamento.Id}", payload);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PutOrcamento_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        var payload = new { valorTotalPecas = 100m, valorTotalInsumos = 50m };

        // Act
        var response = await _client.PutAsJsonAsync("/api/orcamento/99999", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutOrcamento_DeveAtualizarValorTotalGeral()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);
        var orcamento = await CriarOrcamentoViaItemEObterAsync(osId);

        var payload = new { valorTotalPecas = 300m, valorTotalInsumos = 100m };

        // Act
        var putResponse = await _client.PutAsJsonAsync($"/api/orcamento/{orcamento.Id}", payload);
        putResponse.EnsureSuccessStatusCode();

        var getResponse = await _client.GetAsync($"/api/orcamento/{orcamento.Id}");
        getResponse.EnsureSuccessStatusCode();

        // ✅ propriedade corrigida para "getResponse"
        var body = await getResponse.Content.ReadFromJsonAsync<OrcamentoResponseData>();

        // Assert — ValorTotalGeral deve refletir a soma dos novos valores
        Assert.Equal(400m, body!.GetResponse.ValorTotalGeral);
    }

    // ─── DELETE /api/orcamento/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteOrcamento_SemToken_DeveRetornar401()
    {
        // Act
        var response = await _client.DeleteAsync("/api/orcamento/1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteOrcamento_IdExistente_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);
        var orcamento = await CriarOrcamentoViaItemEObterAsync(osId);

        // Act
        var response = await _client.DeleteAsync($"/api/orcamento/{orcamento.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteOrcamento_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.DeleteAsync("/api/orcamento/99999");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteOrcamento_ApósDeletar_GetDeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);
        var orcamento = await CriarOrcamentoViaItemEObterAsync(osId);

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/orcamento/{orcamento.Id}");
        deleteResponse.EnsureSuccessStatusCode();

        var getResponse = await _client.GetAsync($"/api/orcamento/{orcamento.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, getResponse.StatusCode);
    }

    // ─── helpers de desserialização ─────────────────────────────────────────────

    private sealed class IdData
    {
        public int Id { get; set; }
    }

    private sealed class OrcamentoData
    {
        public int     Id                { get; set; }
        public int     OrdemServicoId    { get; set; }
        public decimal ValorTotalPecas   { get; set; }
        public decimal ValorTotalInsumos { get; set; }
        public decimal ValorTotalGeral   { get; set; }
    }

    // ✅ propriedade renomeada de "Response" para "GetResponse" para corresponder
    //    ao nome real serializado pelo ResponseDto<T> ("getResponse")
    private sealed class OrcamentoResponseData
    {
        public OrcamentoData GetResponse { get; set; } = null!;
    }
}