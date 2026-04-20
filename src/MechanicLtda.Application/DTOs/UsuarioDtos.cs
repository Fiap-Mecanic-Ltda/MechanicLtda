using MechanicLtda.Domain.Enums;

namespace MechanicLtda.Application.DTOs
{
    public class UsuarioDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool Ativo { get; set; }
    }

    public class UsuarioCreateDto
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }
        public TipoUsuario Tipo { get; set; }
    }

    public class UsuarioUpdateDto
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public TipoUsuario Tipo { get; set; }
        public bool Ativo { get; set; }
    }
}
