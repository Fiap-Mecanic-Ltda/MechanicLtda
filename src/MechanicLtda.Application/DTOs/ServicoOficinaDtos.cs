namespace MechanicLtda.Application.DTOs
{
    public class ServicoOficinaDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public decimal ValorBase { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataCadastro { get; set; }
        public DateTime? DataAtualizacao { get; set; }
    }

    public class ServicoOficinaCreateDto
    {
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public decimal ValorBase { get; set; }
        public bool Ativo { get; set; } = true;
    }

    public class ServicoOficinaUpdateDto
    {
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public decimal ValorBase { get; set; }
        public bool Ativo { get; set; }
    }
}
