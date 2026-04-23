using System.ComponentModel.DataAnnotations;

namespace MechanicLtda.API.ViewModels
{
    public class ClienteCreateViewModel
    {
        [Required]
        [MaxLength(100)]
        public string Nome { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string? Telefone { get; set; }
    }

    public class ClienteUpdateViewModel
    {
        [Required]
        [MaxLength(100)]
        public string Nome { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string? Telefone { get; set; }

        [Required]
        public bool Ativo { get; set; }
    }
}