using System.ComponentModel.DataAnnotations;

namespace MechanicLtda.API.ViewModels
{
    public class RecusaOrdemServicoViewModel
    {
        [MaxLength(500, ErrorMessage = "O motivo da recusa não pode ultrapassar 500 caracteres.")]
        public string MotivoRecusa { get; set; }
    }

    public class AprovacaoOrdemServicoResponseViewModel
    {
        public int Id { get; set; }
        public string NovoStatus { get; set; }
        public string Mensagem { get; set; }
    }
}
