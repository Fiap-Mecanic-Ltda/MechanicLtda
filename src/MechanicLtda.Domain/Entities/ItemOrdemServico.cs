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

        // Será obrigatório quando a entidade Estoque for implementada
        public int? EstoqueId { get; set; }

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