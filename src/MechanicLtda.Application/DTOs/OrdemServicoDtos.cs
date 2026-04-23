using MechanicLtda.Domain.Enums;

namespace MechanicLtda.Application.DTOs
{
    public class OrdemServicoDto
    {
        public int Id { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataModificacao { get; set; }
        public StatusOrdemServico Status { get; set; }
        public string StatusDescricao => Status.ToString();
        public string DescricaoProblema { get; set; }
        public decimal? ValorTotalEstimado { get; set; }
        public int VeiculoId { get; set; }
        public int ClienteId { get; set; }
    }

    public class OrdemServicoCreateDto
    {
        public string DescricaoProblema { get; set; }
        public decimal? ValorTotalEstimado { get; set; }
        public int VeiculoId { get; set; }
        public int ClienteId { get; set; }
    }

    public class OrdemServicoUpdateDto
    {
        public string DescricaoProblema { get; set; }
        public decimal? ValorTotalEstimado { get; set; }
        public int VeiculoId { get; set; }
        public int ClienteId { get; set; }
    }
}