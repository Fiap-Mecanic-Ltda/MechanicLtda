using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MechanicLtda.Application.DTOs;

namespace MechanicLtda.Web.Integracoes
{
    /// <summary>
    /// Traduz as respostas HTTP dos microsservicos para o ResponseDto que as telas ja usam, para
    /// que FlashErrors e AddErrors continuem valendo. Os servicos devolvem o objeto puro no
    /// sucesso e { success: false, errors: [...] } no erro.
    /// </summary>
    public static class ChamadaServico
    {
        public static readonly JsonSerializerOptions Json = CriarJson();

        private static JsonSerializerOptions CriarJson()
        {
            var opcoes = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            opcoes.Converters.Add(new JsonStringEnumConverter());
            return opcoes;
        }

        public static async Task<ResponseDto<T>> ExecutarAsync<T>(
            string servico,
            Func<Task<HttpResponseMessage>> chamada,
            Func<HttpResponseMessage, Task<T>> lerSucesso)
        {
            var resposta = new ResponseDto<T>();
            try
            {
                using var http = await chamada();
                if (http.IsSuccessStatusCode)
                    return resposta.setResponse(await lerSucesso(http));

                foreach (var erro in await LerErrosAsync(servico, http))
                    resposta.addError(erro);
                return resposta;
            }
            catch (HttpRequestException)
            {
                return resposta.addError($"O {servico} está indisponível no momento. Tente de novo em instantes.");
            }
            catch (TaskCanceledException)
            {
                return resposta.addError($"O {servico} demorou demais para responder. Tente de novo em instantes.");
            }
            catch (JsonException)
            {
                return resposta.addError($"O {servico} respondeu num formato inesperado.");
            }
        }

        public static Task<ResponseDto<T>> ExecutarAsync<T>(string servico, Func<Task<HttpResponseMessage>> chamada) =>
            ExecutarAsync(servico, chamada, async http => (await http.Content.ReadFromJsonAsync<T>(Json))!);

        public static Task<ResponseDto<bool>> ExecutarSemCorpoAsync(string servico, Func<Task<HttpResponseMessage>> chamada) =>
            ExecutarAsync(servico, chamada, _ => Task.FromResult(true));

        private static async Task<IReadOnlyList<string>> LerErrosAsync(string servico, HttpResponseMessage http)
        {
            if (http.StatusCode == HttpStatusCode.Unauthorized)
                return [$"O {servico} não aceitou a sua sessão. Saia e entre de novo."];
            if (http.StatusCode == HttpStatusCode.Forbidden)
                return [$"Seu perfil não tem permissão para esta operação no {servico}."];

            try
            {
                var corpo = await http.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(corpo))
                {
                    using var doc = JsonDocument.Parse(corpo);
                    var raiz = doc.RootElement;
                    if (raiz.ValueKind == JsonValueKind.Object)
                    {
                        if (raiz.TryGetProperty("errors", out var erros))
                        {
                            var mensagens = LerMensagens(erros);
                            if (mensagens.Count > 0)
                                return mensagens;
                        }
                        if (raiz.TryGetProperty("title", out var titulo) && titulo.ValueKind == JsonValueKind.String)
                            return [titulo.GetString()!];
                    }
                }
            }
            catch (JsonException)
            {
                // corpo que nao e JSON: cai na mensagem generica abaixo
            }

            return http.StatusCode == HttpStatusCode.NotFound
                ? ["Registro não encontrado."]
                : [$"O {servico} respondeu {(int)http.StatusCode} ({http.ReasonPhrase})."];
        }

        // "errors" vem como lista (envelope dos servicos) ou como dicionario campo -> lista
        // (ValidationProblemDetails do ASP.NET).
        private static List<string> LerMensagens(JsonElement erros)
        {
            var mensagens = new List<string>();
            if (erros.ValueKind == JsonValueKind.Array)
            {
                mensagens.AddRange(erros.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!));
            }
            else if (erros.ValueKind == JsonValueKind.Object)
            {
                foreach (var campo in erros.EnumerateObject())
                    mensagens.AddRange(LerMensagens(campo.Value));
            }
            return mensagens;
        }
    }
}
