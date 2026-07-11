using System.ComponentModel.DataAnnotations;

namespace MechanicLtda.API.ViewModels
{
    public class ItemOrdemServicoCreateViewModel
    {
        public int? EstoqueId { get; set; }

        public int? ServicoOficinaId { get; set; }

        [MaxLength(1000, ErrorMessage = "A descrição do serviço deve ter no máximo 1000 caracteres.")]
        public string? DescricaoServico { get; set; }

        [Required(ErrorMessage = "A quantidade é obrigatória.")]
        [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
        public int Quantidade { get; set; }

        [Required(ErrorMessage = "O valor unitário é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor unitário deve ser maior que zero.")]
        public decimal ValorUnitario { get; set; }
    }

    /// <summary>
    /// Idêntico a <see cref="ItemOrdemServicoCreateViewModel"/> — mantido como tipo próprio
    /// (em vez de reaproveitar o Create diretamente) para preservar contratos de API
    /// independentes entre POST e PUT, caso venham a divergir no futuro.
    /// </summary>
    public class ItemOrdemServicoUpdateViewModel : ItemOrdemServicoCreateViewModel
    {
    }
}
