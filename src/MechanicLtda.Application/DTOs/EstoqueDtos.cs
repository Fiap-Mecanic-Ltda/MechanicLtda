using MechanicLtda.Domain.Enums;

namespace MechanicLtda.Application.DTOs
{
    public class EstoqueDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public TipoEstoque Tipo { get; set; }
        public int QuantidadeAtual { get; set; }
        public int QuantidadeMinima { get; set; }
        public DateTime DataUltimaAtualizacao { get; set; }
        public bool BaixoEstoque { get; set; }
    }

    public class EstoqueCreateDto
    {
        public string Nome { get; set; }
        public TipoEstoque Tipo { get; set; }
        public int QuantidadeAtual { get; set; }
        public int QuantidadeMinima { get; set; }
    }

    public class EstoqueUpdateDto
    {
        public string Nome { get; set; }
        public TipoEstoque Tipo { get; set; }
        public int QuantidadeMinima { get; set; }
    }

    public class EstoqueReposicaoDto
    {
        public int QuantidadeEntrada { get; set; }
    }
}