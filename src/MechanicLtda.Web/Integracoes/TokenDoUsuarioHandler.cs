using System.Net.Http.Headers;

namespace MechanicLtda.Web.Integracoes
{
    /// <summary>
    /// Repassa aos microsservicos o JWT emitido no login (guardado como claim no cookie) e o
    /// X-Correlation-Id da requisicao, para o rastreio ponta a ponta no New Relic.
    /// </summary>
    public class TokenDoUsuarioHandler : DelegatingHandler
    {
        public const string ClaimToken = "access_token";
        public const string CabecalhoCorrelacao = "X-Correlation-Id";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenDoUsuarioHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var contexto = _httpContextAccessor.HttpContext;
            if (contexto is null)
                return base.SendAsync(request, cancellationToken);

            var token = contexto.User.FindFirst(ClaimToken)?.Value;
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var correlacao = contexto.Request.Headers[CabecalhoCorrelacao].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(correlacao))
                correlacao = contexto.TraceIdentifier;
            request.Headers.TryAddWithoutValidation(CabecalhoCorrelacao, correlacao);

            return base.SendAsync(request, cancellationToken);
        }
    }
}
