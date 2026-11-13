using MechanicLtda.Application.Validations;
using MechanicLtda.Web.Integracoes;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MechanicLtda.Web.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalClientes { get; set; }
        public int TotalVeiculos { get; set; }
        public int TotalOrdensAbertas { get; set; }
        public int TotalItensBaixoEstoque { get; set; }
        public IEnumerable<OrdemServicoDto> OrdensRecentes { get; set; } = [];
        public IEnumerable<EstoqueDto> EstoquesCriticos { get; set; } = [];
    }


    public class LoginRazorViewModel
    {
        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [DataType(DataType.Password)]
        public string Senha { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
    public class ClienteFormViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Informe o nome.")]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telefone { get; set; }

        [Required(ErrorMessage = "Informe o CPF ou CNPJ.")]
        [MaxLength(14)]
        [CpfCnpj(ErrorMessage = "Informe um CPF ou CNPJ válido.")]
        public string CpfCnpj { get; set; } = string.Empty;

        public bool Ativo { get; set; } = true;
    }

    public class VeiculoFormViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Informe a placa.")]
        [MaxLength(8)]
        [PlacaVeiculo(ErrorMessage = "Informe a placa no formato ABC1234 ou ABC1D23.")]
        public string Placa { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a marca.")]
        [MaxLength(50)]
        public string Marca { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o modelo.")]
        [MaxLength(50)]
        public string Modelo { get; set; } = string.Empty;

        [Range(1900, 2100, ErrorMessage = "Informe um ano válido.")]
        public int Ano { get; set; } = DateTime.Now.Year;

        public bool Ativo { get; set; } = true;

        [Range(1, int.MaxValue, ErrorMessage = "Selecione um cliente.")]
        public int ClienteId { get; set; }

        public IEnumerable<SelectListItem> Clientes { get; set; } = [];
    }

    public class EstoqueFormViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Informe o nome.")]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecione o tipo.")]
        public TipoEstoque Tipo { get; set; } = TipoEstoque.Peca;

        [Range(0, int.MaxValue, ErrorMessage = "A quantidade atual não pode ser negativa.")]
        public int QuantidadeAtual { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "A quantidade mínima não pode ser negativa.")]
        public int QuantidadeMinima { get; set; }

        public IEnumerable<SelectListItem> Tipos { get; set; } = [];
    }

    public class EstoqueReposicaoRazorViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Informe uma quantidade maior que zero.")]
        public int QuantidadeEntrada { get; set; } = 1;
    }

    public class ServicoOficinaFormViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Informe o nome do servico.")]
        [MaxLength(120)]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Descricao { get; set; }

        [Range(0, 9999999, ErrorMessage = "Informe um valor base valido.")]
        public decimal ValorBase { get; set; }

        public bool Ativo { get; set; } = true;
    }
    public class OrdemServicoFormViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Descreva o problema informado pelo cliente.")]
        [MaxLength(1000)]
        public string DescricaoProblema { get; set; } = string.Empty;

        [Range(0, 9999999, ErrorMessage = "Informe um valor estimado válido.")]
        public decimal? ValorTotalEstimado { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selecione um veículo.")]
        public int VeiculoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selecione um cliente.")]
        public int ClienteId { get; set; }

        public IEnumerable<SelectListItem> Clientes { get; set; } = [];
        public IEnumerable<SelectListItem> Veiculos { get; set; } = [];
    }

    public class OrdensServicoIndexViewModel
    {
        public IEnumerable<OrdemServicoDto> Ordens { get; set; } = [];
        public TempoMedioExecucaoDto TempoMedioExecucao { get; set; } = new();
    }
    public class OrdemServicoDetalheViewModel
    {
        public OrdemServicoDto Ordem { get; set; } = new();
        public IEnumerable<ItemOrdemServicoDto> Itens { get; set; } = [];
        public IEnumerable<HistoricoStatusOrdemServicoDto> Historico { get; set; } = [];
    }

    public class ItemOrdemServicoFormViewModel
    {
        public int? Id { get; set; }
        public int OrdemServicoId { get; set; }
        public int? EstoqueId { get; set; }
        public int? ServicoOficinaId { get; set; }

        [MaxLength(1000)]
        public string? DescricaoServico { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Informe uma quantidade maior que zero.")]
        public int Quantidade { get; set; } = 1;

        [Range(0.01, 9999999, ErrorMessage = "Informe um valor unitário válido.")]
        public decimal ValorUnitario { get; set; }

        public IEnumerable<SelectListItem> Estoques { get; set; } = [];
        public IEnumerable<SelectListItem> ServicosOficina { get; set; } = [];
    }

}




namespace MechanicLtda.Web.ViewModels
{
    public class OficinaIndexViewModel
    {
        public IEnumerable<FilaExecucaoDto> Fila { get; set; } = [];
        public string? Status { get; set; }
        public bool SagaHabilitada { get; set; }

        public static readonly string[] StatusExecucao =
            ["AguardandoDiagnostico", "EmDiagnostico", "DiagnosticoConcluido", "NaFila", "EmExecucao", "Finalizada", "Cancelada"];
    }

    public class OficinaDetalheViewModel
    {
        public ExecucaoDto Execucao { get; set; } = default!;
        public bool SagaHabilitada { get; set; }
        public DiagnosticoFormViewModel Diagnostico { get; set; } = new();
        public IReadOnlyList<EstoqueDto> Estoques { get; set; } = [];
        public IReadOnlyList<ServicoOficinaDto> Servicos { get; set; } = [];

        public bool Final => Execucao.Status is "Finalizada" or "Cancelada";
    }

    public class DiagnosticoFormViewModel
    {
        public List<LinhaDiagnosticoViewModel> Itens { get; set; } = [];

        [MaxLength(1000)]
        public string? Observacao { get; set; }
    }

    /// <summary>
    /// Uma linha do diagnóstico. "Origem" junta estoque e catálogo num select só:
    /// "E:{id}" (peça ou insumo) ou "S:{id}" (serviço do catálogo). A Execução só aceita
    /// serviço que esteja no catálogo; a descrição é um complemento opcional.
    /// </summary>
    public class LinhaDiagnosticoViewModel
    {
        public string? Origem { get; set; }

        [MaxLength(1000)]
        public string? Descricao { get; set; }

        public int Quantidade { get; set; }
        public decimal? ValorUnitario { get; set; }

        public bool Vazia => string.IsNullOrWhiteSpace(Origem);

        public ItemDiagnosticoDto? Converter(IReadOnlyDictionary<int, TipoEstoque> tiposEstoque)
        {
            var descricao = string.IsNullOrWhiteSpace(Descricao) ? null : Descricao.Trim();
            var partes = (Origem ?? string.Empty).Split(':', 2);

            if (partes.Length == 2 && int.TryParse(partes[1], out var id))
            {
                if (partes[0] == "E" && tiposEstoque.TryGetValue(id, out var tipo))
                    return new ItemDiagnosticoDto(descricao, tipo == TipoEstoque.Peca ? "Peca" : "Insumo", Quantidade, ValorUnitario, id, null);
                if (partes[0] == "S")
                    return new ItemDiagnosticoDto(descricao, "Servico", Quantidade, ValorUnitario, null, id);
            }

            return null;
        }
    }
}
