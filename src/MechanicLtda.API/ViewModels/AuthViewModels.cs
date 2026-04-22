using MechanicLtda.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace MechanicLtda.API.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Senha { get; set; }
    }

    public class RegistrarViewModel
    {
        [Required]
        [MaxLength(50)]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; }

        [Required]
        [MaxLength(50)]
        [MinLength(6)]
        public string Senha { get; set; }

        [Required]
        [Compare(nameof(Senha), ErrorMessage = "As senhas não conferem.")]
        public string ConfirmarSenha { get; set; }

        [Required]
        public TipoUsuario Tipo { get; set; }
    }

    public class AlterarSenhaViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string SenhaAtual { get; set; }

        [Required]
        [MinLength(6)]
        [MaxLength(50)]
        public string NovaSenha { get; set; }

        [Required]
        [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não conferem.")]
        public string ConfirmarNovaSenha { get; set; }
    }
}
