using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace MechanicLtda.Web.Integracoes
{
    // Modelos que o Web troca com o OrdemServico-service e com o ExecucaoProducao-service.
    // Antes viviam no domínio deste repositório; desde que OS, itens, estoque e serviços
    // saíram daqui, são só o formato da conversa com os serviços.

    public enum StatusOrdemServico
    {
        [Display(Name = "Recebida")]
        Recebida = 1,
        [Display(Name = "Diagnóstico")]
        EmDiagnostico = 2,
        [Display(Name = "Aguardando Aprovação")]
        AguardandoAprovacao = 3,
        [Display(Name = "Execução")]
        EmExecucao = 4,
        [Display(Name = "Finalizada")]
        Finalizada = 5,
        [Display(Name = "Entregue")]
        Entregue = 6,
        [Display(Name = "Aguardando Orçamento")]
        AguardandoOrcamento = 7,
        // 8 é reservado (era AguardandoPagamento) e não deve ser usado.
        [Display(Name = "Compensando")]
        Compensando = 9,
        [Display(Name = "Cancelada")]
        Cancelada = 10
    }

    public enum TipoEstoque
    {
        Insumo = 1,
        Peca = 2
    }

    public static class StatusOrdemServicoExtensions
    {
        /// <summary>Nome de exibição do status (atributo Display), ex.: "Diagnóstico".</summary>
        public static string Descricao(this StatusOrdemServico status) =>
            typeof(StatusOrdemServico).GetField(status.ToString())?.GetCustomAttribute<DisplayAttribute>()?.Name
            ?? status.ToString();
    }

    public class OrdemServicoDto
    {
        public int Id { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataModificacao { get; set; }
        public DateTime? DataInicioExecucao { get; set; }
        public DateTime? DataFimExecucao { get; set; }
        public double? TempoExecucaoMinutos { get; set; }
        public StatusOrdemServico Status { get; set; }
        public string StatusDescricao => Status.Descricao();
        public string DescricaoProblema { get; set; } = string.Empty;
        public decimal? ValorTotalEstimado { get; set; }
        public int VeiculoId { get; set; }
        public int ClienteId { get; set; }

        /// <summary>Orçamento e pagamento no Billing, preenchidos pela Saga.</summary>
        public int? OrcamentoId { get; set; }
        public int? PagamentoId { get; set; }
    }

    public class OrdemServicoCreateDto
    {
        public string DescricaoProblema { get; set; } = string.Empty;
        public decimal? ValorTotalEstimado { get; set; }
        public int VeiculoId { get; set; }
        public int ClienteId { get; set; }
    }

    public class OrdemServicoUpdateDto
    {
        public string DescricaoProblema { get; set; } = string.Empty;
        public decimal? ValorTotalEstimado { get; set; }
        public int VeiculoId { get; set; }
        public int ClienteId { get; set; }
    }

    public class TempoMedioExecucaoDto
    {
        public int QuantidadeOrdensFinalizadas { get; set; }
        public double TempoMedioMinutos { get; set; }
        public double TempoMedioHoras { get; set; }
    }

    public record HistoricoStatusOrdemServicoDto(
        int? StatusAnterior, string? StatusAnteriorDescricao, int StatusNovo, string StatusNovoDescricao,
        DateTime DataAlteracao, string Origem, string? Motivo);

    public class ItemOrdemServicoDto
    {
        public int Id { get; set; }
        public int OrdemServicoId { get; set; }
        public int? EstoqueId { get; set; }
        public int? ServicoOficinaId { get; set; }
        public string? DescricaoServico { get; set; }
        public int Quantidade { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal ValorTotal { get; set; }
    }

    public class ItemOrdemServicoCreateDto
    {
        public int? EstoqueId { get; set; }
        public int? ServicoOficinaId { get; set; }
        public string? DescricaoServico { get; set; }
        public int Quantidade { get; set; }
        public decimal ValorUnitario { get; set; }
    }

    public class ItemOrdemServicoUpdateDto : ItemOrdemServicoCreateDto
    {
    }

    public class EstoqueDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public TipoEstoque Tipo { get; set; }
        public int QuantidadeAtual { get; set; }

        /// <summary>Reservado por diagnósticos ainda não pagos.</summary>
        public int QuantidadeReservada { get; set; }
        public int QuantidadeDisponivel => QuantidadeAtual - QuantidadeReservada;
        public int QuantidadeMinima { get; set; }
        public DateTime DataUltimaAtualizacao { get; set; }
        public bool BaixoEstoque { get; set; }
    }

    public class EstoqueCreateDto
    {
        public string Nome { get; set; } = string.Empty;
        public TipoEstoque Tipo { get; set; }
        public int QuantidadeAtual { get; set; }
        public int QuantidadeMinima { get; set; }
    }

    public class EstoqueUpdateDto
    {
        public string Nome { get; set; } = string.Empty;
        public TipoEstoque Tipo { get; set; }
        public int QuantidadeMinima { get; set; }
    }

    public class EstoqueReposicaoDto
    {
        public int QuantidadeEntrada { get; set; }
    }

    public class ServicoOficinaDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public decimal ValorBase { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataCadastro { get; set; }
        public DateTime? DataAtualizacao { get; set; }
    }

    public class ServicoOficinaCreateDto
    {
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public decimal ValorBase { get; set; }
        public bool Ativo { get; set; } = true;
    }

    public class ServicoOficinaUpdateDto : ServicoOficinaCreateDto
    {
    }
}
