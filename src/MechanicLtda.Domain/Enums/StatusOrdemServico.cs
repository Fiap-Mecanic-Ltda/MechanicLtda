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
        Entregue = 6,

        // Status do OrdemServico-service (Saga da Fase 4). Este repositório não grava esses
        // valores; eles existem para o Web exibir as OS que vêm do serviço. O 8 é reservado
        // (era AguardandoPagamento) e não deve ser usado.
        [Display(Name = "Aguardando Orçamento")]
        AguardandoOrcamento = 7,
        [Display(Name = "Compensando")]
        Compensando = 9,
        [Display(Name = "Cancelada")]
        Cancelada = 10
    }
}