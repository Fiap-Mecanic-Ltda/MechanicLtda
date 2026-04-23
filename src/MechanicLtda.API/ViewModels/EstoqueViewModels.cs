using MechanicLtda.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace MechanicLtda.API.ViewModels
{
    public class EstoqueCreateViewModel
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(200, ErrorMessage = "O nome deve ter no máximo 200 caracteres.")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O tipo é obrigatório.")]
        public TipoEstoque Tipo { get; set; }

        [Required(ErrorMessage = "A quantidade atual é obrigatória.")]
        [Range(0, int.MaxValue, ErrorMessage = "A quantidade atual não pode ser negativa.")]
        public int QuantidadeAtual { get; set; }

        [Required(ErrorMessage = "A quantidade mínima é obrigatória.")]
        [Range(0, int.MaxValue, ErrorMessage = "A quantidade mínima não pode ser negativa.")]
        public int QuantidadeMinima { get; set; }
    }

    public class EstoqueUpdateViewModel
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(200, ErrorMessage = "O nome deve ter no máximo 200 caracteres.")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O tipo é obrigatório.")]
        public TipoEstoque Tipo { get; set; }

        [Required(ErrorMessage = "A quantidade mínima é obrigatória.")]
        [Range(0, int.MaxValue, ErrorMessage = "A quantidade mínima não pode ser negativa.")]
        public int QuantidadeMinima { get; set; }
    }

    public class EstoqueReposicaoViewModel
    {
        [Required(ErrorMessage = "A quantidade de entrada é obrigatória.")]
        [Range(1, int.MaxValue, ErrorMessage = "A quantidade de entrada deve ser maior que zero.")]
        public int QuantidadeEntrada { get; set; }
    }
}