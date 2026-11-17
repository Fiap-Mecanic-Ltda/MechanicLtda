using MechanicLtda.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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

        [JsonRequired]
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

        [JsonRequired]
        [Required]
        public TipoUsuario Tipo { get; set; }

        [JsonRequired]
        [Required]
        public bool Ativo { get; set; }
    }
}