using MechanicLtda.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace MechanicLtda.Domain.Entities
{
    public class Cliente : Entity<int>
    {
        [Required]
        [MaxLength(100)]
        public string Nome { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string? Telefone { get; set; }

        [Required]
        [MaxLength(14)]
        public string CpfCnpj { get; set; }

        [Required]
        public bool Ativo { get; set; }

        [Required]
        public DateTime DataCriacao { get; set; }

        public DateTime? DataModificacao { get; set; }

        public ICollection<Veiculo> Veiculos { get; set; } = [];
    }
}
