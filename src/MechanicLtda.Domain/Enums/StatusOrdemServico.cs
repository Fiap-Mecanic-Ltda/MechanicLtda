using System.ComponentModel.DataAnnotations;

namespace MechanicLtda.Domain.Enums
{
    public enum StatusOrdemServico
    {
        [Display(Name = "Recebida")]
        Recebida = 1,
        [Display(Name = "Diagnóstico")]
        EmDiagnostico = 2,
        [Display(Name = "Aguardando Aprovação")]
        AguardandoAprovacao = 3,
        [Display(Name = "Execução")]
        EmExecucao = 4,
        [Display(Name = "Finalizada")]
        Finalizada = 5,
        [Display(Name = "Entregue")]
        Entregue = 6
    }
}