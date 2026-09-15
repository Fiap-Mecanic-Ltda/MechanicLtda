using MechanicLtda.Domain.Entities.Base;
using MechanicLtda.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MechanicLtda.Domain.Entities
{
    public class OrdemServico : Entity<int>
    {
        [Required]
        public DateTime DataCriacao { get; set; }

        public DateTime? DataModificacao { get; set; }

        public DateTime? DataInicioExecucao { get; set; }

        public DateTime? DataFimExecucao { get; set; }

        /// <summary>
        /// Quando a OS entrou no status atual (UTC). Permite medir quanto tempo ela ficou em
        /// cada etapa — Diagnóstico, Execução, Finalização — e não só o intervalo de execução.
        /// Nulo nas OS criadas antes desta coluna existir; nelas a referência é DataCriacao.
        /// </summary>
        public DateTime? DataAlteracaoStatus { get; set; }

        [Required]
        public StatusOrdemServico Status { get; set; } = StatusOrdemServico.Recebida;

        [Required]
        [MaxLength(1000)]
        public string DescricaoProblema { get; set; }

        public decimal? ValorTotalEstimado { get; set; }

        [Required]
        public int VeiculoId { get; set; }

        [ForeignKey(nameof(VeiculoId))]
        public Veiculo Veiculo { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [ForeignKey(nameof(ClienteId))]
        public Cliente Cliente { get; set; }

        public ICollection<ItemOrdemServico> ItensOrdemServico { get; set; } = [];

        public Orcamento? Orcamento { get; set; }

        [NotMapped]
        public TimeSpan? TempoExecucao =>
            DataInicioExecucao.HasValue && DataFimExecucao.HasValue
                ? DataFimExecucao.Value - DataInicioExecucao.Value
                : null;
    }
}
