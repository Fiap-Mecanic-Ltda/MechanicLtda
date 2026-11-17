using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using MechanicLtda.Application.DTOs;

namespace MechanicLtda.Web.Integracoes
{
    // Orçamento e pagamento no OrcamentoPagamento-service (/api/orcamento e /api/pagamento).
    // Contrato em docs/pagamento.md e docs/contratos-eventos.md do serviço. As respostas vêm
    // no envelope ResponseDto do monólito; o status (orçamento e pagamento) vem como número.

    public enum StatusOrcamento
    {
        [Display(Name = "Aberto")] Aberto = 1,
        [Display(Name = "Aprovado")] Aprovado = 2,
        [Display(Name = "Pago")] Pago = 3,
        [Display(Name = "Recusado")] Recusado = 4,
        [Display(Name = "Cancelado")] Cancelado = 5
    }

    public enum StatusPagamento
    {
        [Display(Name = "Pendente")] Pendente = 1,
        [Display(Name = "Em processamento")] EmProcessamento = 2,
        [Display(Name = "Autorizado")] Autorizado = 3,
        [Display(Name = "Aprovado")] Aprovado = 4,
        [Display(Name = "Recusado")] Recusado = 5,
        [Display(Name = "Cancelado")] Cancelado = 6,
        [Display(Name = "Devolvido")] Devolvido = 7,
        [Display(Name = "Em disputa")] EmDisputa = 8,
        [Display(Name = "Estornado")] Estornado = 9
    }

    public enum CategoriaItemOrcamento
    {
        [Display(Name = "Peça")] Peca = 1,
        [Display(Name = "Insumo")] Insumo = 2,
        [Display(Name = "Serviço")] Servico = 3
    }

    public class OrcamentoDto
    {
        public int Id { get; set; }
        public int OrdemServicoId { get; set; }
        public decimal ValorTotalPecas { get; set; }
        public decimal ValorTotalInsumos { get; set; }
        public decimal ValorTotalServicos { get; set; }
        public decimal ValorTotalGeral { get; set; }
        public DateTime DataGeracao { get; set; }
        public DateTime? Validade { get; set; }
        public bool AjustadoManualmente { get; set; }
        public StatusOrcamento Status { get; set; }
        public string? StatusDescricao { get; set; }
        public string? MotivoStatus { get; set; }
        public List<ItemOrcamentoDto> Itens { get; set; } = [];

        /// <summary>Só orçamento Aberto ou Aprovado aceita pagamento, aprovação ou recusa.</summary>
        public bool AceitaDecisao => Status is StatusOrcamento.Aberto or StatusOrcamento.Aprovado;
    }

    public class ItemOrcamentoDto
    {
        public int Id { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public CategoriaItemOrcamento Categoria { get; set; }
        public int Quantidade { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal ValorTotal { get; set; }
    }

    public class PagamentoDto
    {
        public int Id { get; set; }
        public Guid RequisicaoId { get; set; }
        public int OrcamentoId { get; set; }
        public int OrdemServicoId { get; set; }
        public decimal Valor { get; set; }
        public StatusPagamento Status { get; set; }
        public string? StatusDescricao { get; set; }
        public string? StatusDetalhe { get; set; }
        public string? IdExterno { get; set; }
        public int Parcelas { get; set; }
        public string? MetodoPagamento { get; set; }
        public bool ModoTeste { get; set; }
        public string? MotivoCancelamento { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime AtualizadoEm { get; set; }

        /// <summary>Ainda pode mudar no provedor: vale sincronizar.</summary>
        public bool EmAberto => Status is StatusPagamento.Pendente or StatusPagamento.EmProcessamento or StatusPagamento.Autorizado;
    }

    /// <summary>
    /// Corpo do POST /api/pagamento. Sem valor, de propósito: o serviço lê do orçamento.
    /// O token vem do SDK do Mercado Pago no navegador; o cartão nunca passa pelo Web.
    /// </summary>
    public class PagamentoCreateDto
    {
        public Guid RequisicaoId { get; set; }
        public int OrcamentoId { get; set; }
        public string Token { get; set; } = string.Empty;
        public string PaymentMethodId { get; set; } = string.Empty;
        public long? IssuerId { get; set; }
        public int Installments { get; set; } = 1;
        public string? Descricao { get; set; }
        public PagadorDto Payer { get; set; } = new();
    }

    public class PagadorDto
    {
        public string Email { get; set; } = string.Empty;
        public IdentificacaoDto? Identification { get; set; }
    }

    public record IdentificacaoDto(string? Type, string? Number);

    public static class DescricaoEnum
    {
        public static string Descricao<TEnum>(this TEnum valor) where TEnum : struct, Enum =>
            typeof(TEnum).GetField(valor.ToString())?
                .GetCustomAttributes(typeof(DisplayAttribute), false).OfType<DisplayAttribute>().FirstOrDefault()?.Name
            ?? valor.ToString();
    }

    public interface IOrcamentoCliente
    {
        /// <summary>Orçamento da OS. Resposta sem erro e sem conteúdo quando ainda não foi gerado.</summary>
        Task<ResponseDto<OrcamentoDto?>> ObterPorOrdemServicoAsync(int ordemServicoId);
        Task<ResponseDto<OrcamentoDto>> ObterPorIdAsync(int id);
        Task<ResponseDto<byte[]>> ObterPdfAsync(int id);
        Task<ResponseDto<OrcamentoDto>> AprovarAsync(int id);
        Task<ResponseDto<OrcamentoDto>> RecusarAsync(int id, string? motivo);
    }

    public interface IPagamentoCliente
    {
        Task<ResponseDto<PagamentoDto>> CriarAsync(PagamentoCreateDto dto);
        Task<ResponseDto<IEnumerable<PagamentoDto>>> ListarPorOrcamentoAsync(int orcamentoId);

        /// <summary>Consulta o provedor e reaplica o status. Sem conteúdo quando nada mudou.</summary>
        Task<ResponseDto<PagamentoDto?>> SincronizarAsync(int id);
    }

    public class OrcamentoHttpCliente : IOrcamentoCliente
    {
        public const string NomeServico = "serviço de Orçamento e Pagamento";
        private const string Rota = "api/orcamento";

        private readonly HttpClient _http;

        public OrcamentoHttpCliente(HttpClient http)
        {
            _http = http;
        }

        public async Task<ResponseDto<OrcamentoDto?>> ObterPorOrdemServicoAsync(int ordemServicoId)
        {
            // 404 aqui é o caminho normal enquanto a Saga não pediu o orçamento.
            var naoGerado = false;
            var resposta = await ChamadaServico.ExecutarEnvelopeAsync<OrcamentoDto>(NomeServico, async () =>
            {
                var http = await _http.GetAsync($"{Rota}/ordem-servico/{ordemServicoId}");
                naoGerado = http.StatusCode == HttpStatusCode.NotFound;
                return http;
            });

            var destino = new ResponseDto<OrcamentoDto?>();
            if (naoGerado)
                return destino;
            if (resposta.hasErrors)
            {
                resposta.getErrors.ForEach(e => destino.addError(e));
                return destino;
            }
            return destino.setResponse(resposta.getResponse);
        }

        public Task<ResponseDto<OrcamentoDto>> ObterPorIdAsync(int id) =>
            ChamadaServico.ExecutarEnvelopeAsync<OrcamentoDto>(NomeServico, () => _http.GetAsync($"{Rota}/{id}"));

        public Task<ResponseDto<byte[]>> ObterPdfAsync(int id) =>
            ChamadaServico.ExecutarAsync(NomeServico, () => _http.GetAsync($"{Rota}/{id}/pdf"),
                http => http.Content.ReadAsByteArrayAsync());

        public Task<ResponseDto<OrcamentoDto>> AprovarAsync(int id) =>
            ChamadaServico.ExecutarEnvelopeAsync<OrcamentoDto>(NomeServico,
                () => _http.PatchAsync($"{Rota}/{id}/aprovar", null));

        public Task<ResponseDto<OrcamentoDto>> RecusarAsync(int id, string? motivo) =>
            ChamadaServico.ExecutarEnvelopeAsync<OrcamentoDto>(NomeServico,
                () => _http.PatchAsJsonAsync($"{Rota}/{id}/recusar",
                    new { Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim() }, ChamadaServico.Json));
    }

    public class PagamentoHttpCliente : IPagamentoCliente
    {
        private const string Rota = "api/pagamento";

        private readonly HttpClient _http;

        public PagamentoHttpCliente(HttpClient http)
        {
            _http = http;
        }

        public Task<ResponseDto<PagamentoDto>> CriarAsync(PagamentoCreateDto dto) =>
            ChamadaServico.ExecutarEnvelopeAsync<PagamentoDto>(OrcamentoHttpCliente.NomeServico,
                () => _http.PostAsJsonAsync(Rota, dto, ChamadaServico.Json));

        public async Task<ResponseDto<IEnumerable<PagamentoDto>>> ListarPorOrcamentoAsync(int orcamentoId)
        {
            var resposta = await ChamadaServico.ExecutarEnvelopeAsync<List<PagamentoDto>>(OrcamentoHttpCliente.NomeServico,
                () => _http.GetAsync($"{Rota}/orcamento/{orcamentoId}"));
            return OrdemServicoHttpAppService.Converter<List<PagamentoDto>, IEnumerable<PagamentoDto>>(resposta, x => x);
        }

        public async Task<ResponseDto<PagamentoDto?>> SincronizarAsync(int id)
        {
            var destino = new ResponseDto<PagamentoDto?>();
            var semMudanca = false;
            var resposta = await ChamadaServico.ExecutarEnvelopeAsync<PagamentoDto>(OrcamentoHttpCliente.NomeServico, async () =>
            {
                var http = await _http.PostAsync($"{Rota}/{id}/sincronizar", null);
                semMudanca = http.StatusCode == HttpStatusCode.NoContent;
                return http;
            });

            if (semMudanca)
                return destino;
            if (resposta.hasErrors)
            {
                resposta.getErrors.ForEach(e => destino.addError(e));
                return destino;
            }
            return destino.setResponse(resposta.getResponse);
        }
    }

    /// <summary>Usado quando Integracoes:OrcamentoService:BaseUrl não está configurada.</summary>
    public class OrcamentoPagamentoNaoConfigurado : IOrcamentoCliente, IPagamentoCliente
    {
        public const string Mensagem =
            "O serviço de Orçamento e Pagamento não está configurado neste ambiente (Integracoes:OrcamentoService:BaseUrl).";

        private static ResponseDto<T> Erro<T>() => new ResponseDto<T>().addError(Mensagem);

        public Task<ResponseDto<OrcamentoDto?>> ObterPorOrdemServicoAsync(int ordemServicoId) => Task.FromResult(Erro<OrcamentoDto?>());
        public Task<ResponseDto<OrcamentoDto>> ObterPorIdAsync(int id) => Task.FromResult(Erro<OrcamentoDto>());
        public Task<ResponseDto<byte[]>> ObterPdfAsync(int id) => Task.FromResult(Erro<byte[]>());
        public Task<ResponseDto<OrcamentoDto>> AprovarAsync(int id) => Task.FromResult(Erro<OrcamentoDto>());
        public Task<ResponseDto<OrcamentoDto>> RecusarAsync(int id, string? motivo) => Task.FromResult(Erro<OrcamentoDto>());
        public Task<ResponseDto<PagamentoDto>> CriarAsync(PagamentoCreateDto dto) => Task.FromResult(Erro<PagamentoDto>());
        public Task<ResponseDto<IEnumerable<PagamentoDto>>> ListarPorOrcamentoAsync(int orcamentoId) => Task.FromResult(Erro<IEnumerable<PagamentoDto>>());
        public Task<ResponseDto<PagamentoDto?>> SincronizarAsync(int id) => Task.FromResult(Erro<PagamentoDto?>());
    }
}
