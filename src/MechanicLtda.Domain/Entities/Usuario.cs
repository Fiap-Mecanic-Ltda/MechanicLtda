using MechanicLtda.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MechanicLtda.Domain.Entities
{
    public class Usuario : IdentityUser
    {
        [Required]
        public TipoUsuario Tipo { get; set; }

        [Required]
        public bool Ativo { get; set; }
        
        [Required]
        public DateTime DataCriacao { get; set; }
        
        public DateTime? DataModificacao { get; set; }
    }
}