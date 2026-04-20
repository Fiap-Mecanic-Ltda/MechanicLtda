using MechanicLtda.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace MechanicLtda.API.ViewModels
{
    public class UsuarioCreateViewModel
    {
        [Required]
        [MaxLength(50)]
        public string UserName { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; }

        [Required]
        [MaxLength(50)]
        public string Senha { get; set; }

        [Required]
        public TipoUsuario Tipo { get; set; }
    }

    public class UsuarioUpdateViewModel
    {
        [Required]
        [MaxLength(50)]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; }

        [Required]
        public TipoUsuario Tipo { get; set; }

        [Required]
        public bool Ativo { get; set; }
    }
}