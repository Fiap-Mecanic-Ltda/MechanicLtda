using MechanicLtda.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MechanicLtda.Domain.Entities
{
    public class ItemOrdemServico : Entity<int>
    {
        [Required]
        public int OrdemServicoId { get; set; }

        [ForeignKey(nameof(OrdemServicoId))]
        public OrdemServico OrdemServico { get; set; }

        public int? EstoqueId { get; set; }

        [ForeignKey(nameof(EstoqueId))]
        public Estoque? Estoque { get; set; }

        public int? ServicoOficinaId { get; set; }

        [ForeignKey(nameof(ServicoOficinaId))]
        public ServicoOficina? ServicoOficina { get; set; }

        [MaxLength(1000)]
        public string? DescricaoServico { get; set; }

        [Required]
        public int Quantidade { get; set; }

        [Required]
        public decimal ValorUnitario { get; set; }

        public decimal ValorTotal { get; set; }

        public void CalcularValorTotal()
        {
            ValorTotal = Quantidade * ValorUnitario;
        }
    }
}
