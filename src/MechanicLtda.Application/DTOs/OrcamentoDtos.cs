namespace MechanicLtda.Application.DTOs
{
    public class OrcamentoDto
    {
        public int Id { get; set; }
        public int OrdemServicoId { get; set; }
        public decimal ValorTotalPecas { get; set; }
        public decimal ValorTotalInsumos { get; set; }
        public decimal ValorTotalGeral { get; set; }
        public DateTime DataGeracao { get; set; }
        public DateTime? Validade { get; set; }
    }

    public class OrcamentoUpdateDto
    {
        public decimal ValorTotalPecas { get; set; }
        public decimal ValorTotalInsumos { get; set; }
        public DateTime? Validade { get; set; }
    }
}