namespace MechanicLtda.Web.Integracoes
{
    /// <summary>
    /// Liga o Web aos microsservicos da Fase 4. Cada servico tem a sua chave: desligada (o
    /// padrao), o Web continua usando o codigo deste repositorio em processo, como antes.
    /// Ligada, as telas daquele contexto passam a chamar o servico por HTTP, repassando o JWT
    /// do usuario logado e o X-Correlation-Id.
    /// </summary>
    public class IntegracoesOptions
    {
        public const string Secao = "Integracoes";

        /// <summary>OrdemServico-service: OS, itens, status e historico.</summary>
        public ServicoRemotoOptions OsService { get; set; } = new();

        /// <summary>ExecucaoProducao-service: estoque, servicos da oficina e fila de execucao.</summary>
        public ServicoRemotoOptions ExecucaoService { get; set; } = new();
    }

    public class ServicoRemotoOptions
    {
        public bool Habilitado { get; set; }

        /// <summary>
        /// Endereco base do servico. No cluster e o API Gateway (ou o Service do k8s); no
        /// docker-compose de integracao, o nome do container.
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        public int TimeoutSegundos { get; set; } = 10;
    }
}
