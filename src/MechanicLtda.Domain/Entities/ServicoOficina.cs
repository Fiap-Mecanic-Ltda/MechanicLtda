using MechanicLtda.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace MechanicLtda.Domain.Entities
{
    public class ServicoOficina : Entity<int>
    {
        [Required]
        [MaxLength(200)]
        public string Nome { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Descricao { get; set; }

        [Required]
        public decimal ValorBase { get; set; }

        public bool Ativo { get; set; } = true;

        public DateTime DataCadastro { get; set; }

        public DateTime? DataAtualizacao { get; set; }

        public ICollection<ItemOrdemServico> ItensOrdemServico { get; set; } = [];
    }
}
