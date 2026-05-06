using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MechanicLtda.API.IntegrationTests.Helpers;
using Xunit;

namespace MechanicLtda.API.IntegrationTests.Controllers;

public class OrdemServicoControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions _jsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public OrdemServicoControllerTests(CustomWebApplicationFactory factory)
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

    /// <summary>
    /// Gera uma placa no formato antigo (ABC1234) única por teste,
    /// derivada de bytes do Guid para evitar colisões.
    /// </summary>
    private static string GerarPlacaUnica()
    {
        var b = Guid.NewGuid().ToByteArray();
        var l1 = (char)('A' + b[0] % 26);
        var l2 = (char)('A' + b[1] % 26);
        var l3 = (char)('A' + b[2] % 26);
        var n  = (b[3] * 256 + b[4]) % 10000;
        return $"{l1}{l2}{l3}{n:D4}";
    }

    /// <summary>
    /// Cria um cliente via POST e devolve o Id retornado pela API.
    /// </summary>
    private async Task<int> CriarClienteEObterIdAsync()
    {
        var payload = new
        {
            nome     = "Cliente Teste OS",
            email    = $"cliente.os.{Guid.NewGuid():N}@teste.com",
            cpfCnpj  = "52998224725",
            telefone = "11999999999"
        };

        var response = await _client.PostAsJsonAsync("/api/cliente", payload);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<IdData>(_jsonOptions);
        return body!.Id;
    }

    /// <summary>
    /// Cria um veículo vinculado ao cliente informado e devolve o Id retornado pela API.
    /// </summary>
    private async Task<int> CriarVeiculoEObterIdAsync(int clienteId)
    {
        var payload = new
        {
            placa     = GerarPlacaUnica(),
            marca     = "Toyota",
            modelo    = "Corolla",
            ano       = 2022,
            clienteId = clienteId,
            ativo     = true,
            dataCriacao = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync("/api/veiculo", payload);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<IdData>(_jsonOptions);
        return body!.Id;
    }

    private static object PayloadOSValido(int veiculoId, int clienteId, decimal? valorEstimado = 500m) =>
        new
        {
            descricaoProblema  = "Barulho no motor",
            valorTotalEstimado = valorEstimado,
            veiculoId          = veiculoId,
            clienteId          = clienteId
        };

    /// <summary>
    /// Cria uma OS via POST e devolve o Id retornado pela API.
    /// </summary>
    private async Task<int> CriarOSEObterIdAsync(int veiculoId, int clienteId)
    {
        var response = await _client.PostAsJsonAsync("/api/ordemservico", PayloadOSValido(veiculoId, clienteId));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<IdData>(_jsonOptions);
        return body!.Id;
    }

    // ─── GET /api/ordemservico ───────────────────────────────────────────────────

    [Fact]
    public async Task GetOrdemServico_SemToken_DeveRetornar401()
    {
        // Act
        var response = await _client.GetAsync("/api/ordemservico");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrdemServico_ComToken_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.GetAsync("/api/ordemservico");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─── GET /api/ordemservico/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task GetOrdemServicoPorId_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.GetAsync("/api/ordemservico/99999");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrdemServicoPorId_IdExistente_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);

        // Act
        var response = await _client.GetAsync($"/api/ordemservico/{osId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─── GET /api/ordemservico/cliente/{clienteId} ───────────────────────────────

    [Fact]
    public async Task GetOrdemServicoPorCliente_ClienteComOS_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        await CriarOSEObterIdAsync(veiculoId, clienteId);

        // Act
        var response = await _client.GetAsync($"/api/ordemservico/cliente/{clienteId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOrdemServicoPorCliente_ClienteIdInvalido_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.GetAsync("/api/ordemservico/cliente/nao-e-numero");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── POST /api/ordemservico ──────────────────────────────────────────────────

    [Fact]
    public async Task PostOrdemServico_DadosValidos_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/ordemservico", PayloadOSValido(veiculoId, clienteId));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostOrdemServico_VeiculoInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        var payload = new
        {
            descricaoProblema  = "Descrição qualquer",
            valorTotalEstimado = 100m,
            veiculoId          = 99999,
            clienteId          = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ordemservico", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostOrdemServico_VeiculoNaoPertenceAoCliente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();
        var cliente1Id = await CriarClienteEObterIdAsync();
        var cliente2Id = await CriarClienteEObterIdAsync();
        var veiculoId  = await CriarVeiculoEObterIdAsync(cliente1Id);

        var payload = new
        {
            descricaoProblema  = "Descrição qualquer",
            valorTotalEstimado = 100m,
            veiculoId          = veiculoId,
            clienteId          = cliente2Id
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ordemservico", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostOrdemServico_SemDescricao_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);

        var payload = new { veiculoId, clienteId };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ordemservico", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── PUT /api/ordemservico/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task PutOrdemServico_DadosValidos_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);

        var payload = new
        {
            descricaoProblema  = "Descrição atualizada",
            valorTotalEstimado = 750m,
            veiculoId          = veiculoId,
            clienteId          = clienteId
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/ordemservico/{osId}", payload);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PutOrdemServico_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        var payload = new
        {
            descricaoProblema  = "Descrição",
            valorTotalEstimado = 100m,
            veiculoId          = 1,
            clienteId          = 1
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/ordemservico/99999", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── DELETE /api/ordemservico/{id} ───────────────────────────────────────────

    [Fact]
    public async Task DeleteOrdemServico_IdExistente_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);

        // Act
        var response = await _client.DeleteAsync($"/api/ordemservico/{osId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteOrdemServico_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.DeleteAsync("/api/ordemservico/99999");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── PATCH /api/ordemservico/{id}/iniciar-diagnostico ────────────────────────

    [Fact]
    public async Task PatchIniciarDiagnostico_OSRecebida_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);

        // Act
        var response = await _client.PatchAsync($"/api/ordemservico/{osId}/iniciar-diagnostico", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PatchIniciarDiagnostico_IdInexistente_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();

        // Act
        var response = await _client.PatchAsync("/api/ordemservico/99999/iniciar-diagnostico", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── PATCH /api/ordemservico/{id}/aguardar-aprovacao ────────────────────────

    [Fact]
    public async Task PatchAguardarAprovacao_OSEmDiagnostico_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);

        await _client.PatchAsync($"/api/ordemservico/{osId}/iniciar-diagnostico", null);

        // Act
        var response = await _client.PatchAsync($"/api/ordemservico/{osId}/aguardar-aprovacao", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PatchAguardarAprovacao_OSNaoEmDiagnostico_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);

        // OS ainda está 'Recebida' — transição inválida
        var response = await _client.PatchAsync($"/api/ordemservico/{osId}/aguardar-aprovacao", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── PATCH /api/ordemservico/{id}/iniciar-execucao ──────────────────────────

    [Fact]
    public async Task PatchIniciarExecucao_OSAguardandoAprovacao_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);

        await _client.PatchAsync($"/api/ordemservico/{osId}/iniciar-diagnostico", null);
        await _client.PatchAsync($"/api/ordemservico/{osId}/aguardar-aprovacao", null);

        // Act
        var response = await _client.PatchAsync($"/api/ordemservico/{osId}/iniciar-execucao", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PatchIniciarExecucao_OSNaoAguardandoAprovacao_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);

        // OS está 'Recebida' — transição inválida
        var response = await _client.PatchAsync($"/api/ordemservico/{osId}/iniciar-execucao", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── PATCH /api/ordemservico/{id}/finalizar ──────────────────────────────────

    [Fact]
    public async Task PatchFinalizar_OSEmExecucao_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);

        await _client.PatchAsync($"/api/ordemservico/{osId}/iniciar-diagnostico", null);
        await _client.PatchAsync($"/api/ordemservico/{osId}/aguardar-aprovacao", null);
        await _client.PatchAsync($"/api/ordemservico/{osId}/iniciar-execucao", null);

        // Act
        var response = await _client.PatchAsync($"/api/ordemservico/{osId}/finalizar", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PatchFinalizar_OSNaoEmExecucao_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);

        // OS está 'Recebida' — transição inválida
        var response = await _client.PatchAsync($"/api/ordemservico/{osId}/finalizar", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── PATCH /api/ordemservico/{id}/entregar ───────────────────────────────────

    [Fact]
    public async Task PatchEntregar_OSFinalizada_DeveRetornar200()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);

        await _client.PatchAsync($"/api/ordemservico/{osId}/iniciar-diagnostico", null);
        await _client.PatchAsync($"/api/ordemservico/{osId}/aguardar-aprovacao", null);
        await _client.PatchAsync($"/api/ordemservico/{osId}/iniciar-execucao", null);
        await _client.PatchAsync($"/api/ordemservico/{osId}/finalizar", null);

        // Act
        var response = await _client.PatchAsync($"/api/ordemservico/{osId}/entregar", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PatchEntregar_OSNaoFinalizada_DeveRetornar400()
    {
        // Arrange
        await AutenticarAsync();
        var clienteId = await CriarClienteEObterIdAsync();
        var veiculoId = await CriarVeiculoEObterIdAsync(clienteId);
        var osId      = await CriarOSEObterIdAsync(veiculoId, clienteId);

        // OS está 'Recebida' — transição inválida
        var response = await _client.PatchAsync($"/api/ordemservico/{osId}/entregar", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── helpers de desserialização ─────────────────────────────────────────────

    private sealed class IdData
    {
        public int Id { get; set; }
    }
}