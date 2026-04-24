using MechanicLtda.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MechanicLtda.Domain.Entities
{
    public class Orcamento : Entity<int>
    {
        [Required]
        public int OrdemServicoId { get; set; }

        [ForeignKey(nameof(OrdemServicoId))]
        public OrdemServico OrdemServico { get; set; }

        [Required]
        public decimal ValorTotalPecas { get; set; }

        [Required]
        public decimal ValorTotalInsumos { get; set; }

        [Required]
        public decimal ValorTotalGeral { get; set; }

        [Required]
        public DateTime DataGeracao { get; set; }

        public DateTime? Validade { get; set; }
    }
}