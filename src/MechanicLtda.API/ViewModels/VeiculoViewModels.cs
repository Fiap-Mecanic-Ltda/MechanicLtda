using System.ComponentModel.DataAnnotations;

namespace MechanicLtda.API.ViewModels
{
    public class VeiculoCreateViewModel
    {
        [Required]
        [MaxLength(10)]
        public string Placa { get; set; }

        [Required]
        [MaxLength(50)]
        public string Marca { get; set; }

        [Required]
        [MaxLength(50)]
        public string Modelo { get; set; }

        [Required]
        [Range(1900, 2100)]
        public int Ano { get; set; }

        [Required]
        public int ClienteId { get; set; }
    }

    public class VeiculoUpdateViewModel
    {
        [Required]
        [MaxLength(10)]
        public string Placa { get; set; }

        [Required]
        [MaxLength(50)]
        public string Marca { get; set; }

        [Required]
        [MaxLength(50)]
        public string Modelo { get; set; }

        [Required]
        [Range(1900, 2100)]
        public int Ano { get; set; }

        [Required]
        public bool Ativo { get; set; }

        [Required]
        public int ClienteId { get; set; }
    }
}