using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace MechanicLtda.API.Validations
{
    public class CpfCnpjAttribute : ValidationAttribute
    {
        private static readonly Regex NaoDigitoRegex =
            new(@"[^\d]", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

        public CpfCnpjAttribute()
            : base("O campo {0} deve conter um CPF ou CNPJ valido.") { }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
                return ValidationResult.Success;

            var documento = NaoDigitoRegex.Replace(value.ToString()!, "");

            var valido = documento.Length switch
            {
                11 => ValidarCpf(documento),
                14 => ValidarCnpj(documento),
                _  => false
            };

            return valido
                ? ValidationResult.Success
                : new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
        }

        private static bool ValidarCpf(string cpf)
        {
            if (cpf.Distinct().Count() == 1) return false;

            int Soma(int[] pesos) =>
                pesos.Select((p, i) => int.Parse(cpf[i].ToString()) * p).Sum();

            int Digito(int soma)
            {
                var resto = soma % 11;
                return resto < 2 ? 0 : 11 - resto;
            }

            var d1 = Digito(Soma([10, 9, 8, 7, 6, 5, 4, 3, 2]));
            var d2 = Digito(Soma([11, 10, 9, 8, 7, 6, 5, 4, 3, 2]));

            return cpf[9] - '0' == d1 && cpf[10] - '0' == d2;
        }

        private static bool ValidarCnpj(string cnpj)
        {
            if (cnpj.Distinct().Count() == 1) return false;

            int Soma(int[] pesos) =>
                pesos.Select((p, i) => int.Parse(cnpj[i].ToString()) * p).Sum();

            int Digito(int soma)
            {
                var resto = soma % 11;
                return resto < 2 ? 0 : 11 - resto;
            }

            var d1 = Digito(Soma([5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]));
            var d2 = Digito(Soma([6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]));

            return cnpj[12] - '0' == d1 && cnpj[13] - '0' == d2;
        }
    }
}