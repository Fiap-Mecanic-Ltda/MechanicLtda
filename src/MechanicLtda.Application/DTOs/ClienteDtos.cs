namespace MechanicLtda.Application.DTOs
{
    public class ClienteDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public string? Telefone { get; set; }
        public bool Ativo { get; set; }
    }

    public class ClienteCreateDto
    {
        public string Nome { get; set; }
        public string Email { get; set; }
        public string? Telefone { get; set; }
    }

    public class ClienteUpdateDto
    {
        public string Nome { get; set; }
        public string Email { get; set; }
        public string? Telefone { get; set; }
        public bool Ativo { get; set; }
    }
}