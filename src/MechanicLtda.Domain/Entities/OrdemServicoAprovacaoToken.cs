using MechanicLtda.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MechanicLtda.Domain.Entities
{
    public class OrdemServicoAprovacaoToken : Entity<int>
    {
        [Required]
        public int OrdemServicoId { get; set; }

        [ForeignKey(nameof(OrdemServicoId))]
        public OrdemServico OrdemServico { get; set; }

        [Required]
        [MaxLength(64)]
        public string Token { get; set; }

        [Required]
        public DateTime DataCriacao { get; set; }

        [Required]
        public DateTime DataExpiracao { get; set; }

        [Required]
        public bool Utilizado { get; set; } = false;

        public DateTime? DataUtilizacao { get; set; }
    }
}
