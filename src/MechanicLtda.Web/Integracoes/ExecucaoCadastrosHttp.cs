using System.Net.Http.Json;
using MechanicLtda.Application.DTOs;

namespace MechanicLtda.Web.Integracoes
{
    /// <summary>
    /// Estoque no ExecucaoProducao-service (/api/estoque). O diagnóstico reserva e só o
    /// início da execução baixa.
    /// </summary>
    public class EstoqueHttpAppService : IEstoqueCliente
    {
        public const string NomeServico = "serviço de Execução";
        private const string Rota = "api/estoque";

        private readonly HttpClient _http;

        public EstoqueHttpAppService(HttpClient http)
        {
            _http = http;
        }

        public Task<ResponseDto<EstoqueDto>> AdicionarAsync(EstoqueCreateDto dto) =>
            Item(() => _http.PostAsJsonAsync(Rota, new { dto.Nome, dto.Tipo, dto.QuantidadeAtual, dto.QuantidadeMinima }, ChamadaServico.Json));

        public Task<ResponseDto<EstoqueDto>> AtualizarAsync(string id, EstoqueUpdateDto dto) =>
            Item(() => _http.PutAsJsonAsync($"{Rota}/{id}", new { dto.Nome, dto.Tipo, dto.QuantidadeMinima }, ChamadaServico.Json));

        public Task<ResponseDto<bool>> RemoverAsync(string id) =>
            ChamadaServico.ExecutarSemCorpoAsync(NomeServico, () => _http.DeleteAsync($"{Rota}/{id}"));

        public Task<ResponseDto<IEnumerable<EstoqueDto>>> ObterTodosAsync() => Lista(() => _http.GetAsync(Rota));

        public Task<ResponseDto<EstoqueDto>> ObterPorIdAsync(string id) => Item(() => _http.GetAsync($"{Rota}/{id}"));

        public Task<ResponseDto<EstoqueDto>> ReporQuantidadeAsync(string id, EstoqueReposicaoDto dto) =>
            Item(() => _http.PatchAsJsonAsync($"{Rota}/{id}/reposicao", new { Quantidade = dto.QuantidadeEntrada }, ChamadaServico.Json));

        private async Task<ResponseDto<EstoqueDto>> Item(Func<Task<HttpResponseMessage>> chamada) =>
            await ChamadaServico.ExecutarAsync<EstoqueDto>(NomeServico, chamada);

        private async Task<ResponseDto<IEnumerable<EstoqueDto>>> Lista(Func<Task<HttpResponseMessage>> chamada)
        {
            var resposta = await ChamadaServico.ExecutarAsync<List<EstoqueDto>>(NomeServico, chamada);
            return OrdemServicoHttpAppService.Converter<List<EstoqueDto>, IEnumerable<EstoqueDto>>(resposta, x => x);
        }
    }

    /// <summary>Catálogo de serviços no ExecucaoProducao-service (/api/servicos-oficina).</summary>
    public class ServicoOficinaHttpAppService : IServicoOficinaCliente
    {
        private const string Rota = "api/servicos-oficina";

        private readonly HttpClient _http;

        public ServicoOficinaHttpAppService(HttpClient http)
        {
            _http = http;
        }

        public Task<ResponseDto<ServicoOficinaDto>> AdicionarAsync(ServicoOficinaCreateDto dto) =>
            Item(() => _http.PostAsJsonAsync(Rota, new { dto.Nome, dto.Descricao, dto.ValorBase, dto.Ativo }, ChamadaServico.Json));

        public Task<ResponseDto<ServicoOficinaDto>> AtualizarAsync(string id, ServicoOficinaUpdateDto dto) =>
            Item(() => _http.PutAsJsonAsync($"{Rota}/{id}", new { dto.Nome, dto.Descricao, dto.ValorBase, dto.Ativo }, ChamadaServico.Json));

        public Task<ResponseDto<bool>> RemoverAsync(string id) =>
            ChamadaServico.ExecutarSemCorpoAsync(EstoqueHttpAppService.NomeServico, () => _http.DeleteAsync($"{Rota}/{id}"));

        public Task<ResponseDto<IEnumerable<ServicoOficinaDto>>> ObterTodosAsync() => Lista(() => _http.GetAsync(Rota));

        public Task<ResponseDto<IEnumerable<ServicoOficinaDto>>> ObterAtivosAsync() => Lista(() => _http.GetAsync($"{Rota}/ativos"));

        public Task<ResponseDto<ServicoOficinaDto>> ObterPorIdAsync(string id) => Item(() => _http.GetAsync($"{Rota}/{id}"));

        private async Task<ResponseDto<ServicoOficinaDto>> Item(Func<Task<HttpResponseMessage>> chamada) =>
            await ChamadaServico.ExecutarAsync<ServicoOficinaDto>(EstoqueHttpAppService.NomeServico, chamada);

        private async Task<ResponseDto<IEnumerable<ServicoOficinaDto>>> Lista(Func<Task<HttpResponseMessage>> chamada)
        {
            var resposta = await ChamadaServico.ExecutarAsync<List<ServicoOficinaDto>>(EstoqueHttpAppService.NomeServico, chamada);
            return OrdemServicoHttpAppService.Converter<List<ServicoOficinaDto>, IEnumerable<ServicoOficinaDto>>(resposta, x => x);
        }
    }
}
