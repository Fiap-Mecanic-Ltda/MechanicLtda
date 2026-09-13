using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace MechanicLtda.Domain.Enums
{
    public static class StatusOrdemServicoExtensions
    {
        /// <summary>
        /// Nome de exibição do status (atributo Display), ex.: "Diagnóstico", "Execução".
        /// É o texto usado nos e-mails ao cliente e nos painéis de monitoramento.
        /// </summary>
        public static string Descricao(this StatusOrdemServico status)
        {
            var campo = typeof(StatusOrdemServico).GetField(status.ToString());

            return campo?.GetCustomAttribute<DisplayAttribute>()?.Name ?? status.ToString();
        }
    }
}
