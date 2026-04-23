using MechanicLtda.Domain.Entities.Base;
using MechanicLtda.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace MechanicLtda.Domain.Entities
{
    public class Estoque : Entity<int>
    {
        [Required]
        [MaxLength(200)]
        public string Nome { get; set; }

        [Required]
        public TipoEstoque Tipo { get; set; }

        [Required]
        public int QuantidadeAtual { get; set; }

        [Required]
        public int QuantidadeMinima { get; set; }

        public DateTime DataUltimaAtualizacao { get; set; }

        public ICollection<ItemOrdemServico> ItensOrdemServico { get; set; } = [];

        /// <summary>
        /// Indica se o estoque está abaixo ou igual à quantidade mínima configurada.
        /// </summary>
        public bool EstaBaixoEstoque() => QuantidadeAtual <= QuantidadeMinima;
    }
}