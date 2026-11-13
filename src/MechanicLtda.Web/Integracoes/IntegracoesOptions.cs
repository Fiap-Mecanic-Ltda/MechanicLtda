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
}
