using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using MechanicLtda.API.IntegrationTests.Helpers;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace MechanicLtda.API.IntegrationTests.Controllers;

/// <summary>
/// Cobre as duas garantias que sustentam a autenticação por CPF no API Gateway:
/// a API aceita o token emitido pela Function serverless (segundo issuer) e um cliente
/// autenticado só consegue ler as próprias ordens de serviço.
/// </summary>
public class OrdemServicoPosseTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string IssuerDaLambda = "MechanicLtda.Auth.Cpf";
    private const string AudienceDeTeste = "MechanicLtdaUsers";

    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions _jsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public OrdemServicoPosseTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ─── helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reproduz o token que a Lambda de autenticação por CPF emite: mesma chave simétrica
    /// da API, issuer próprio, role Cliente e o claim clienteId.
    /// </summary>
    private static string GerarTokenDeCliente(int clienteId)
    {
        var chave = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")!;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, clienteId.ToString()),
            new("clienteId", clienteId.ToString()),
            new(ClaimTypes.Role, "Cliente")
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject            = new ClaimsIdentity(claims),
            Issuer             = IssuerDaLambda,
            Audience           = AudienceDeTeste,
            Expires            = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JwtSecurityTokenHandler();

        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    private async Task<HttpClient> CriarClienteAutenticadoComoAdminAsync()
    {
        var client = _factory.CreateClient();
        var token  = await AuthHelper.ObterTokenAsync(client, "admin@mechanic.com", "Admin@123");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private async Task<int> CriarClienteEObterIdAsync()
    {
        var admin = await CriarClienteAutenticadoComoAdminAsync();

        var payload = new
        {
            nome     = "Cliente Posse OS",
            email    = $"cliente.posse.{Guid.NewGuid():N}@teste.com",
            cpfCnpj  = "52998224725",
            telefone = "11999999999"
        };

        var response = await admin.PostAsJsonAsync("/api/cliente", payload);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var dto  = JsonSerializer.Deserialize<IdData>(json, _jsonOptions)!;

        return dto.Id;
    }

    private HttpClient CriarClienteComTokenDeCliente(int clienteId)
    {
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GerarTokenDeCliente(clienteId));

        return client;
    }

    // ─── testes ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPorCliente_ComTokenDaLambdaDoProprioCliente_DeveRetornar200()
    {
        var clienteId = await CriarClienteEObterIdAsync();
        var client    = CriarClienteComTokenDeCliente(clienteId);

        var response = await client.GetAsync($"/api/ordemservico/cliente/{clienteId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPorCliente_ComTokenDeOutroCliente_DeveRetornar403()
    {
        var clienteId = await CriarClienteEObterIdAsync();
        var client    = CriarClienteComTokenDeCliente(clienteId);

        var response = await client.GetAsync($"/api/ordemservico/cliente/{clienteId + 1000}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPorCliente_ComoAdministrador_DeveRetornar200ParaQualquerCliente()
    {
        var clienteId = await CriarClienteEObterIdAsync();
        var admin     = await CriarClienteAutenticadoComoAdminAsync();

        var response = await admin.GetAsync($"/api/ordemservico/cliente/{clienteId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPorCliente_SemToken_DeveRetornar401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/ordemservico/cliente/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record IdData(int Id);
}
