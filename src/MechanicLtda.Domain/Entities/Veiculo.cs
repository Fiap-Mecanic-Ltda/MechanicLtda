using MechanicLtda.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MechanicLtda.Domain.Entities
{
    public class Veiculo : Entity<int>
    {

        [Required]
        [MaxLength(10)]
        public string Placa { get; set; }

        [Required]
        [MaxLength(50)]
        public string Marca { get; set; }

        [Required]
        [MaxLength(50)]
        public string Modelo { get; set; }

        [Required]
        public int Ano { get; set; }

        [Required]
        public bool Ativo { get; set; }

        [Required]
        public DateTime DataCriacao { get; set; }

        public DateTime? DataModificacao { get; set; }

        // Relacionamento N:1 com Cliente
        [Required]
        public int ClienteId { get; set; }

        [ForeignKey(nameof(ClienteId))]
        public Cliente Cliente { get; set; }
    }
}