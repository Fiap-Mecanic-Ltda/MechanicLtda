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

        [Required]
        public StatusOrdemServico Status { get; set; } = StatusOrdemServico.EmAberto;

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

        // Preparado para 1:N com ItensServico (futura implementação)
        // public ICollection<ItemServico> ItensServico { get; set; } = [];
    }
}