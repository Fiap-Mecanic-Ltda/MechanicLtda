using System.ComponentModel.DataAnnotations;

namespace MechanicLtda.Domain.Entities
{
    public class Cliente
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nome { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string? Telefone { get; set; }

        [Required]
        public bool Ativo { get; set; }

        [Required]
        public DateTime DataCriacao { get; set; }

        public DateTime? DataModificacao { get; set; }
    }
}
