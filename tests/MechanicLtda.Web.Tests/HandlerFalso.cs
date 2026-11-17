using System.Net;
using System.Text;

namespace MechanicLtda.Web.Tests
{
    /// <summary>Responde às chamadas HTTP com o que o teste mandar e guarda o que foi enviado.</summary>
    public class HandlerFalso : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _respostas = new();

        public List<(HttpMethod Metodo, string Url, string? Corpo)> Chamadas { get; } = [];

        public HandlerFalso Responder(HttpStatusCode status, string? json = null)
        {
            _respostas.Enqueue(_ => new HttpResponseMessage(status)
            {
                Content = new StringContent(json ?? string.Empty, Encoding.UTF8, "application/json")
            });
            return this;
        }

        public HandlerFalso Falhar(Exception erro)
        {
            _respostas.Enqueue(_ => throw erro);
            return this;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var corpo = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Chamadas.Add((request.Method, request.RequestUri!.PathAndQuery, corpo));
            return _respostas.Dequeue()(request);
        }

        public HttpClient Cliente() => new(this) { BaseAddress = new Uri("http://servico/") };
    }
}
