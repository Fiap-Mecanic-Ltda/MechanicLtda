namespace MechanicLtda.Web.Integracoes
{
    /// <summary>
    /// Endereços dos microsserviços da Fase 4. Ordem de serviço, itens, estoque e serviços da
    /// oficina vivem só neles; o Web chama por HTTP, repassando o JWT do usuário logado e o
    /// X-Correlation-Id.
    /// </summary>
    public class IntegracoesOptions
    {
        public const string Secao = "Integracoes";

        /// <summary>OrdemServico-service: OS, itens, status e histórico.</summary>
        public ServicoRemotoOptions OsService { get; set; } = new();

        /// <summary>ExecucaoProducao-service: estoque, serviços da oficina e fila de execução.</summary>
        public ServicoRemotoOptions ExecucaoService { get; set; } = new();

        /// <summary>
        /// OrcamentoPagamento-service: orçamento, PDF, aprovação/recusa e pagamento. No cluster
        /// a BaseUrl é a do API Gateway (ADR-003 do serviço), que encaminha /api/orcamento e
        /// /api/pagamento para ele. Opcional: sem ela o Web sobe e a tela de orçamento avisa.
        /// </summary>
        public OrcamentoServicoOptions OrcamentoService { get; set; } = new();

        /// <summary>
        /// Se a Saga do OS Service está ligada (o padrão). Desligada, a tela da oficina mostra
        /// os botões manuais da Execução (receber a OS, liberar a execução), que a Saga faria.
        /// </summary>
        public bool SagaHabilitada { get; set; } = true;
    }

    public class ServicoRemotoOptions
    {
        /// <summary>
        /// Endereço base do serviço. No cluster é o API Gateway (ou o Service do k8s); no
        /// docker-compose, o nome do container.
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        public int TimeoutSegundos { get; set; } = 10;
    }

    public class OrcamentoServicoOptions : ServicoRemotoOptions
    {
        /// <summary>
        /// Public key (de teste) do Mercado Pago, usada pelo SDK JS para tokenizar o cartão no
        /// navegador. Vazia, a tela de pagamento entra no modo simulado: manda um token de
        /// teste (APRO, CONT, FUND...) que só o gateway fake do serviço entende.
        /// </summary>
        public string MercadoPagoPublicKey { get; set; } = string.Empty;

        public bool Configurado => Uri.TryCreate(BaseUrl, UriKind.Absolute, out _);
    }
}
