namespace MechanicLtda.Application.DTOs
{
    public class OrcamentoDto
    {
        public int Id { get; set; }
        public int OrdemServicoId { get; set; }
        public decimal ValorTotalPecas { get; set; }
        public decimal ValorTotalInsumos { get; set; }
        public decimal ValorTotalGeral { get; set; }
        public DateTime DataGeracao { get; set; }
        public DateTime? Validade { get; set; }
    }

    public class OrcamentoUpdateDto
    {
        public decimal ValorTotalPecas { get; set; }
        public decimal ValorTotalInsumos { get; set; }
        public DateTime? Validade { get; set; }
    }

    public class OrcamentoPdfDto
    {
        public int Id { get; set; }
        public int OrdemServicoId { get; set; }
        public DateTime DataGeracao { get; set; }
        public DateTime? Validade { get; set; }

        public decimal ValorTotalPecas { get; set; }
        public decimal ValorTotalInsumos { get; set; }
        public decimal ValorTotalGeral { get; set; }

        // Ordem de Serviço
        public string DescricaoProblema { get; set; }
        public string StatusOrdemServico { get; set; }

        // Cliente
        public string ClienteNome { get; set; }
        public string ClienteEmail { get; set; }
        public string? ClienteTelefone { get; set; }

        // Veículo
        public string VeiculoMarca { get; set; }
        public string VeiculoModelo { get; set; }
        public string VeiculoPlaca { get; set; }
        public int VeiculoAno { get; set; }
    }
}