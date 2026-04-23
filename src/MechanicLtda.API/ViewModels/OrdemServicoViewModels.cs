using System.ComponentModel.DataAnnotations;

namespace MechanicLtda.API.ViewModels
{
    public class OrdemServicoCreateViewModel
    {
        [Required(ErrorMessage = "A descrição do problema é obrigatória.")]
        [MaxLength(1000, ErrorMessage = "A descrição não pode ultrapassar 1000 caracteres.")]
        public string DescricaoProblema { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "O valor estimado deve ser maior que zero.")]
        public decimal? ValorTotalEstimado { get; set; }

        [Required(ErrorMessage = "O VeiculoId é obrigatório.")]
        public int VeiculoId { get; set; }

        [Required(ErrorMessage = "O ClienteId é obrigatório.")]
        public int ClienteId { get; set; }
    }

    public class OrdemServicoUpdateViewModel
    {
        [Required(ErrorMessage = "A descrição do problema é obrigatória.")]
        [MaxLength(1000, ErrorMessage = "A descrição não pode ultrapassar 1000 caracteres.")]
        public string DescricaoProblema { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "O valor estimado deve ser maior que zero.")]
        public decimal? ValorTotalEstimado { get; set; }

        [Required(ErrorMessage = "O VeiculoId é obrigatório.")]
        public int VeiculoId { get; set; }

        [Required(ErrorMessage = "O ClienteId é obrigatório.")]
        public int ClienteId { get; set; }
    }
}