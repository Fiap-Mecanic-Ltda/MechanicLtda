namespace MechanicLtda.Application.DTOs
{
    public class VeiculoDto
    {
        public int Id { get; set; }
        public string Placa { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public int Ano { get; set; }
        public bool Ativo { get; set; }
        public int ClienteId { get; set; }
    }

    public class VeiculoCreateDto
    {
        public string Placa { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public int Ano { get; set; }
        public int ClienteId { get; set; }
    }

    public class VeiculoUpdateDto
    {
        public string Placa { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public int Ano { get; set; }
        public bool Ativo { get; set; }
        public int ClienteId { get; set; }
    }
}