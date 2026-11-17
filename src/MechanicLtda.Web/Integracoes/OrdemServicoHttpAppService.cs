using System.Net.Http.Json;
using MechanicLtda.Application.DTOs;

namespace MechanicLtda.Web.Integracoes
{
    /// <summary>
    /// Cliente do OrdemServico-service (/api/ordemservico/*). Com a Saga ligada no serviço, as
    /// transições que ela comanda respondem 409 com a explicação.
    /// </summary>
    public class OrdemServicoHttpAppService : IOrdemServicoCliente
    {
        public const string NomeServico = "serviço de Ordem de Serviço";
        private const string Rota = "api/ordemservico";

        private readonly HttpClient _http;

        public OrdemServicoHttpAppService(HttpClient http)
        {
            _http = http;
        }

        public Task<ResponseDto<OrdemServicoDto>> AdicionarAsync(OrdemServicoCreateDto dto) =>
            Ordem(() => _http.PostAsJsonAsync(Rota, new
            {
                dto.DescricaoProblema,
                dto.ValorTotalEstimado,
                dto.VeiculoId,
                dto.ClienteId
            }, ChamadaServico.Json));

        public async Task<ResponseDto<OrdemServicoDto>> AtualizarAsync(string id, OrdemServicoUpdateDto dto)
        {
            // O OS Service não troca cliente nem veículo depois da abertura.
            var atual = await ObterPorIdAsync(id);
            if (atual.hasErrors)
                return atual;
            if (atual.getResponse.ClienteId != dto.ClienteId || atual.getResponse.VeiculoId != dto.VeiculoId)
                return new ResponseDto<OrdemServicoDto>()
                    .addError("Cliente e veículo não podem mudar depois da abertura da OS. Cancele e abra uma nova.");

            return await Ordem(() => _http.PutAsJsonAsync($"{Rota}/{id}", new
            {
                dto.DescricaoProblema,
                dto.ValorTotalEstimado
            }, ChamadaServico.Json));
        }

        public Task<ResponseDto<OrdemServicoDto>> IniciarDiagnosticoAsync(string id) => Transicao(id, "iniciar-diagnostico");
        public Task<ResponseDto<OrdemServicoDto>> AguardarAprovacaoAsync(string id) => Transicao(id, "aguardar-aprovacao");
        public Task<ResponseDto<OrdemServicoDto>> AprovarAsync(string id) => Transicao(id, "aprovar");
        public Task<ResponseDto<OrdemServicoDto>> IniciarExecucaoAsync(string id) => Transicao(id, "iniciar-execucao");
        public Task<ResponseDto<OrdemServicoDto>> FinalizarAsync(string id) => Transicao(id, "finalizar");
        public Task<ResponseDto<OrdemServicoDto>> EntregarAsync(string id) => Transicao(id, "entregar");

        public Task<ResponseDto<OrdemServicoDto>> RecusarAsync(string id, string motivoRecusa) =>
            Transicao(id, "recusar", new { MotivoRecusa = string.IsNullOrWhiteSpace(motivoRecusa) ? null : motivoRecusa });

        public Task<ResponseDto<OrdemServicoDto>> CancelarAsync(int id, string? motivo) =>
            Transicao(id.ToString(), "cancelar", new { Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo });

        public async Task<ResponseDto<IEnumerable<OrdemServicoDto>>> ObterTodosAsync() =>
            await Lista(() => _http.GetAsync(Rota));

        public Task<ResponseDto<OrdemServicoDto>> ObterPorIdAsync(string id) =>
            Ordem(() => _http.GetAsync($"{Rota}/{id}"));

        public Task<ResponseDto<TempoMedioExecucaoDto>> ObterTempoMedioExecucaoAsync() =>
            ChamadaServico.ExecutarAsync<TempoMedioExecucaoDto>(NomeServico, () => _http.GetAsync($"{Rota}/tempo-medio-execucao"));

        public Task<ResponseDto<bool>> RemoverAsync(string id) =>
            ChamadaServico.ExecutarSemCorpoAsync(NomeServico, () => _http.DeleteAsync($"{Rota}/{id}"));

        public async Task<ResponseDto<IEnumerable<HistoricoStatusOrdemServicoDto>>> ObterHistoricoAsync(int id)
        {
            var resposta = await ChamadaServico.ExecutarAsync<List<HistoricoStatusOrdemServicoDto>>(
                NomeServico, () => _http.GetAsync($"{Rota}/{id}/historico"));
            return Converter<List<HistoricoStatusOrdemServicoDto>, IEnumerable<HistoricoStatusOrdemServicoDto>>(resposta, x => x);
        }

        private Task<ResponseDto<OrdemServicoDto>> Transicao(string id, string acao, object? corpo = null) =>
            Ordem(() => _http.PatchAsJsonAsync($"{Rota}/{id}/{acao}", corpo, ChamadaServico.Json));

        private async Task<ResponseDto<OrdemServicoDto>> Ordem(Func<Task<HttpResponseMessage>> chamada)
        {
            var resposta = await ChamadaServico.ExecutarAsync<OrdemServicoRemota>(NomeServico, chamada);
            return Converter(resposta, Mapear);
        }

        private async Task<ResponseDto<IEnumerable<OrdemServicoDto>>> Lista(Func<Task<HttpResponseMessage>> chamada)
        {
            var resposta = await ChamadaServico.ExecutarAsync<List<OrdemServicoRemota>>(NomeServico, chamada);
            return Converter<List<OrdemServicoRemota>, IEnumerable<OrdemServicoDto>>(resposta, x => x.Select(Mapear).ToList());
        }

        internal static ResponseDto<TDestino> Converter<TOrigem, TDestino>(ResponseDto<TOrigem> origem, Func<TOrigem, TDestino> mapa)
        {
            var destino = new ResponseDto<TDestino>();
            if (origem.hasErrors)
            {
                origem.getErrors.ForEach(e => destino.addError(e));
                return destino;
            }
            return destino.setResponse(mapa(origem.getResponse));
        }

        private static OrdemServicoDto Mapear(OrdemServicoRemota os) => new()
        {
            Id = os.Id,
            DataCriacao = os.DataCriacao,
            DataModificacao = os.DataModificacao,
            DataInicioExecucao = os.DataInicioExecucao,
            DataFimExecucao = os.DataFimExecucao,
            TempoExecucaoMinutos = os.TempoExecucaoMinutos,
            Status = (StatusOrdemServico)os.Status,
            DescricaoProblema = os.DescricaoProblema,
            ValorTotalEstimado = os.ValorTotalEstimado,
            VeiculoId = os.VeiculoId,
            ClienteId = os.ClienteId,
            OrcamentoId = os.OrcamentoId,
            PagamentoId = os.PagamentoId
        };

        /// <summary>Formato da OrdemServicoDto do OS Service (status como número).</summary>
        private sealed record OrdemServicoRemota(
            int Id, DateTime DataCriacao, DateTime? DataModificacao, DateTime? DataInicioExecucao,
            DateTime? DataFimExecucao, double? TempoExecucaoMinutos, int Status, string DescricaoProblema,
            decimal? ValorTotalEstimado, int VeiculoId, int ClienteId, int? OrcamentoId, int? PagamentoId);
    }
}
