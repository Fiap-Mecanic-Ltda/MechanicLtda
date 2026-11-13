using System.Net.Http.Json;
using MechanicLtda.Application.DTOs;

namespace MechanicLtda.Web.Integracoes
{
    /// <summary>Fila de execução do ExecucaoProducao-service, usada pela tela da oficina.</summary>
    public interface IExecucaoCliente
    {
        Task<ResponseDto<IEnumerable<FilaExecucaoDto>>> FilaAsync(string? status);
        Task<ResponseDto<ExecucaoDto>> ObterAsync(int ordemServicoId);
        Task<ResponseDto<ExecucaoDto>> ReceberAsync(int ordemServicoId, int? clienteId, int? veiculoId, string? descricaoProblema);
        Task<ResponseDto<ExecucaoDto>> IniciarDiagnosticoAsync(int ordemServicoId);
        Task<ResponseDto<ExecucaoDto>> RegistrarDiagnosticoAsync(int ordemServicoId, IReadOnlyList<ItemDiagnosticoDto> itens, string? observacao);
        Task<ResponseDto<ExecucaoDto>> FalhaDiagnosticoAsync(int ordemServicoId, string motivo);
        Task<ResponseDto<ExecucaoDto>> LiberarExecucaoAsync(int ordemServicoId);
        Task<ResponseDto<ExecucaoDto>> IniciarAsync(int ordemServicoId);
        Task<ResponseDto<ExecucaoDto>> FinalizarAsync(int ordemServicoId, string? observacao);
        Task<ResponseDto<ExecucaoDto>> CancelarAsync(int ordemServicoId, string? motivo);
    }

    public record FilaExecucaoDto(int OrdemServicoId, string Status, string? DescricaoProblema, DateTime DataEntrada, DateTime DataAlteracaoStatus);

    public record ItemExecucaoDto(string Descricao, string Tipo, int Quantidade, decimal ValorUnitario, decimal ValorTotal, int? EstoqueId, int? ServicoOficinaId);

    public record HistoricoExecucaoDto(string? StatusAnterior, string StatusNovo, DateTime DataHora, string? Observacao);

    public record ExecucaoDto(
        int OrdemServicoId, int? ClienteId, int? VeiculoId, string? DescricaoProblema, string Status,
        string? Observacao, string? MotivoCancelamento, DateTime DataEntrada, DateTime DataAlteracaoStatus,
        DateTime? DataConclusaoDiagnostico, DateTime? DataInicio, DateTime? DataFim, decimal ValorTotalItens,
        IReadOnlyList<ItemExecucaoDto> Itens, IReadOnlyList<HistoricoExecucaoDto> Historico);

    /// <summary>Item que o mecânico registra no diagnóstico. Tipo: Peca, Insumo ou Servico.</summary>
    public record ItemDiagnosticoDto(string? Descricao, string Tipo, int Quantidade, decimal? ValorUnitario, int? EstoqueId, int? ServicoOficinaId);

    public class ExecucaoHttpCliente : IExecucaoCliente
    {
        private const string Rota = "api/execucao";
        private readonly HttpClient _http;

        public ExecucaoHttpCliente(HttpClient http)
        {
            _http = http;
        }

        public async Task<ResponseDto<IEnumerable<FilaExecucaoDto>>> FilaAsync(string? status)
        {
            var url = string.IsNullOrWhiteSpace(status) ? $"{Rota}/fila" : $"{Rota}/fila?status={Uri.EscapeDataString(status)}";
            var resposta = await ChamadaServico.ExecutarAsync<List<FilaExecucaoDto>>(EstoqueHttpAppService.NomeServico, () => _http.GetAsync(url));
            return OrdemServicoHttpAppService.Converter<List<FilaExecucaoDto>, IEnumerable<FilaExecucaoDto>>(resposta, x => x);
        }

        public Task<ResponseDto<ExecucaoDto>> ObterAsync(int ordemServicoId) =>
            Execucao(() => _http.GetAsync($"{Rota}/{ordemServicoId}"));

        public Task<ResponseDto<ExecucaoDto>> ReceberAsync(int ordemServicoId, int? clienteId, int? veiculoId, string? descricaoProblema) =>
            Execucao(() => _http.PostAsJsonAsync(Rota, new { ordemServicoId, clienteId, veiculoId, descricaoProblema }, ChamadaServico.Json));

        public Task<ResponseDto<ExecucaoDto>> IniciarDiagnosticoAsync(int ordemServicoId) => Patch(ordemServicoId, "iniciar-diagnostico");

        public Task<ResponseDto<ExecucaoDto>> RegistrarDiagnosticoAsync(int ordemServicoId, IReadOnlyList<ItemDiagnosticoDto> itens, string? observacao) =>
            Execucao(() => _http.PostAsJsonAsync($"{Rota}/{ordemServicoId}/diagnostico", new { itens, observacao }, ChamadaServico.Json));

        public Task<ResponseDto<ExecucaoDto>> FalhaDiagnosticoAsync(int ordemServicoId, string motivo) =>
            Patch(ordemServicoId, "falha-diagnostico", new { motivo });

        public Task<ResponseDto<ExecucaoDto>> LiberarExecucaoAsync(int ordemServicoId) => Patch(ordemServicoId, "liberar-execucao");

        public Task<ResponseDto<ExecucaoDto>> IniciarAsync(int ordemServicoId) => Patch(ordemServicoId, "iniciar");

        public Task<ResponseDto<ExecucaoDto>> FinalizarAsync(int ordemServicoId, string? observacao) =>
            Patch(ordemServicoId, "finalizar", new { observacao });

        public Task<ResponseDto<ExecucaoDto>> CancelarAsync(int ordemServicoId, string? motivo) =>
            Patch(ordemServicoId, "cancelar", new { motivo });

        private Task<ResponseDto<ExecucaoDto>> Patch(int ordemServicoId, string acao, object? corpo = null) =>
            Execucao(() => _http.PatchAsJsonAsync($"{Rota}/{ordemServicoId}/{acao}", corpo, ChamadaServico.Json));

        private static Task<ResponseDto<ExecucaoDto>> Execucao(Func<Task<HttpResponseMessage>> chamada) =>
            ChamadaServico.ExecutarAsync<ExecucaoDto>(EstoqueHttpAppService.NomeServico, chamada);
    }
}
