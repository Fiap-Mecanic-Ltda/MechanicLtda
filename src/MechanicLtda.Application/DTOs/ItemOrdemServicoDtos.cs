namespace MechanicLtda.Application.DTOs
{
    public class ItemOrdemServicoDto
    {
        public int Id { get; set; }
        public int OrdemServicoId { get; set; }
        public int? EstoqueId { get; set; }
        public int Quantidade { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal ValorTotal { get; set; }
    }

    public class ItemOrdemServicoCreateDto
    {
        public int? EstoqueId { get; set; }
        public int Quantidade { get; set; }
        public decimal ValorUnitario { get; set; }
    }

    public class ItemOrdemServicoUpdateDto
    {
        public int? EstoqueId { get; set; }
        public int Quantidade { get; set; }
        public decimal ValorUnitario { get; set; }
    }
}