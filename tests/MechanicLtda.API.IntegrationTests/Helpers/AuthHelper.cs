using System.Net.Http.Json;
using System.Text.Json;

namespace MechanicLtda.API.IntegrationTests.Helpers;

public static class AuthHelper
{
    public static async Task<string> ObterTokenAsync(
        HttpClient client,
        string email,
        string senha)
    {
        var payload = new { email, senha };

        var response = await client.PostAsJsonAsync("/api/auth/login", payload);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Login falhou com status {(int)response.StatusCode} ({response.StatusCode}). " +
                $"Corpo da resposta: {body}");
        }

        var json      = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement.GetProperty("token").GetString()!;
    }
}