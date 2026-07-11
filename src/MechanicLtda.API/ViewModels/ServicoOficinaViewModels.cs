using System.ComponentModel.DataAnnotations;

namespace MechanicLtda.API.ViewModels
{
    public class ServicoOficinaCreateViewModel
    {
        [Required(ErrorMessage = "O nome do serviço é obrigatório.")]
        [MaxLength(200, ErrorMessage = "O nome deve ter no máximo 200 caracteres.")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "A descrição do serviço é obrigatória.")]
        [MaxLength(1000, ErrorMessage = "A descrição deve ter no máximo 1000 caracteres.")]
        public string Descricao { get; set; }

        [Required(ErrorMessage = "O valor base é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor base deve ser maior que zero.")]
        public decimal ValorBase { get; set; }

        public bool Ativo { get; set; } = true;
    }

    public class ServicoOficinaUpdateViewModel
    {
        [Required(ErrorMessage = "O nome do serviço é obrigatório.")]
        [MaxLength(200, ErrorMessage = "O nome deve ter no máximo 200 caracteres.")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "A descrição do serviço é obrigatória.")]
        [MaxLength(1000, ErrorMessage = "A descrição deve ter no máximo 1000 caracteres.")]
        public string Descricao { get; set; }

        [Required(ErrorMessage = "O valor base é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor base deve ser maior que zero.")]
        public decimal ValorBase { get; set; }

        public bool Ativo { get; set; }
    }
}
