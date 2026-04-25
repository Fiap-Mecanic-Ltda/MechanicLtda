using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace MechanicLtda.API.Validations
{
    /// <summary>
    /// Valida placa no formato antigo (ABC1234) ou Mercosul (ABC1D23).
    /// </summary>
    public partial class PlacaVeiculoAttribute : ValidationAttribute
    {
        // Formato antigo:  3 letras + 4 dígitos          ex.: ABC1234
        // Formato Mercosul: 3 letras + 1 dígito + 1 letra + 2 dígitos  ex.: ABC1D23
        [GeneratedRegex(@"^[A-Za-z]{3}[0-9]{4}$|^[A-Za-z]{3}[0-9][A-Za-z][0-9]{2}$")]
        private static partial Regex PlacaRegex();

        public PlacaVeiculoAttribute()
            : base("O campo {0} deve conter uma placa válida no formato antigo (ABC1234) ou Mercosul (ABC1D23).") { }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
                return ValidationResult.Success;

            return PlacaRegex().IsMatch(value.ToString()!)
                ? ValidationResult.Success
                : new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
        }
    }
}