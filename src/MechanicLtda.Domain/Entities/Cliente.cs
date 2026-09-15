using MechanicLtda.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace MechanicLtda.Domain.Entities
{
    public class Cliente : Entity<int>
    {
        [Required]
        [MaxLength(100)]
        public string Nome { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string? Telefone { get; set; }

        [Required]
        [MaxLength(14)]
        public string CpfCnpj { get; set; }

        /// <summary>
        /// Índice cego do CPF/CNPJ (HMAC-SHA256 dos dígitos, em hexadecimal).
        /// Existe porque CpfCnpj é cifrado com IV aleatório e não pode ser pesquisado por
        /// igualdade: a autenticação por CPF localiza o cliente por este campo.
        /// Nulo apenas em linhas gravadas antes do backfill.
        /// </summary>
        [MaxLength(64)]
        public string? CpfCnpjHash { get; set; }

        [Required]
        public bool Ativo { get; set; }

        [Required]
        public DateTime DataCriacao { get; set; }

        public DateTime? DataModificacao { get; set; }

        public ICollection<Veiculo> Veiculos { get; set; } = [];
    }
}
