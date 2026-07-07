using System.ComponentModel.DataAnnotations;
using MechanicLtda.Domain.Enums;

namespace MechanicLtda.Application.DTOs
{
    public class OrdemServicoDto
    {
        public int Id { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataModificacao { get; set; }
        public DateTime? DataInicioExecucao { get; set; }
        public DateTime? DataFimExecucao { get; set; }
        public double? TempoExecucaoMinutos { get; set; }
        public StatusOrdemServico Status { get; set; }
        public string StatusDescricao => GetDisplayName(Status);
        public string DescricaoProblema { get; set; }
        public decimal? ValorTotalEstimado { get; set; }
        public int VeiculoId { get; set; }
        public int ClienteId { get; set; }

        private static string GetDisplayName(Enum value)
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value);
            if (name == null) return value.ToString();

            var field = type.GetField(name);
            if (field == null) return value.ToString();

            var displayAttribute = field.GetCustomAttributes(typeof(DisplayAttribute), false)
                .FirstOrDefault() as DisplayAttribute;

            return displayAttribute?.Name ?? value.ToString();
        }
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

    public class TempoMedioExecucaoDto
    {
        public int QuantidadeOrdensFinalizadas { get; set; }
        public double TempoMedioMinutos { get; set; }
        public double TempoMedioHoras { get; set; }
    }
}
