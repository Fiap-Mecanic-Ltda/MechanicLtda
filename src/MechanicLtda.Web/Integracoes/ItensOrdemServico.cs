using System.Net.Http.Json;
using MechanicLtda.Application.DTOs;

namespace MechanicLtda.Web.Integracoes
{
    /// <summary>
    /// Itens no OS Service. O serviço não tem chave estrangeira para estoque e serviços: cada
    /// item leva tipo (Peca, Insumo, Servico), descrição e valor. Quem classifica é o Web, a
    /// partir do estoque e do catálogo de serviços da Execução.
    /// </summary>
    public class ItensOrdemServicoHttp : IItensOrdemServicoCliente
    {
        private readonly HttpClient _http;
        private readonly IEstoqueCliente _estoque;
        private readonly IServicoOficinaCliente _servicos;

        public ItensOrdemServicoHttp(HttpClient http, IEstoqueCliente estoque, IServicoOficinaCliente servicos)
        {
            _http = http;
            _estoque = estoque;
            _servicos = servicos;
        }

        private static string Rota(int ordemServicoId) => $"api/ordem-servico/{ordemServicoId}/itens";

        public async Task<ResponseDto<IEnumerable<ItemOrdemServicoDto>>> ListarAsync(int ordemServicoId)
        {
            var resposta = await ChamadaServico.ExecutarAsync<List<ItemRemoto>>(
                OrdemServicoHttpAppService.NomeServico, () => _http.GetAsync(Rota(ordemServicoId)));
            return OrdemServicoHttpAppService.Converter<List<ItemRemoto>, IEnumerable<ItemOrdemServicoDto>>(
                resposta, x => x.Select(i => Mapear(ordemServicoId, i)).ToList());
        }

        public Task<ResponseDto<ItemOrdemServicoDto>> ObterAsync(int ordemServicoId, int id) =>
            Item(ordemServicoId, () => _http.GetAsync($"{Rota(ordemServicoId)}/{id}"));

        public async Task<ResponseDto<ItemOrdemServicoDto>> AdicionarAsync(int ordemServicoId, ItemOrdemServicoCreateDto dto)
        {
            var corpo = await MontarAsync(dto.EstoqueId, dto.ServicoOficinaId, dto.DescricaoServico, dto.Quantidade, dto.ValorUnitario);
            if (corpo.hasErrors)
                return OrdemServicoHttpAppService.Converter<ItemEnvio, ItemOrdemServicoDto>(corpo, _ => null!);

            return await Item(ordemServicoId, () => _http.PostAsJsonAsync(Rota(ordemServicoId), corpo.getResponse, ChamadaServico.Json));
        }

        public async Task<ResponseDto<ItemOrdemServicoDto>> AtualizarAsync(int ordemServicoId, int id, ItemOrdemServicoUpdateDto dto)
        {
            var corpo = await MontarAsync(dto.EstoqueId, dto.ServicoOficinaId, dto.DescricaoServico, dto.Quantidade, dto.ValorUnitario);
            if (corpo.hasErrors)
                return OrdemServicoHttpAppService.Converter<ItemEnvio, ItemOrdemServicoDto>(corpo, _ => null!);

            return await Item(ordemServicoId, () => _http.PutAsJsonAsync($"{Rota(ordemServicoId)}/{id}", corpo.getResponse, ChamadaServico.Json));
        }

        public Task<ResponseDto<bool>> RemoverAsync(int ordemServicoId, int id) =>
            ChamadaServico.ExecutarSemCorpoAsync(OrdemServicoHttpAppService.NomeServico,
                () => _http.DeleteAsync($"{Rota(ordemServicoId)}/{id}"));

        /// <summary>
        /// Mesma classificação que o Billing espera (docs/microsservicos/orcamento.md): estoque
        /// do tipo Peca ou Insumo; serviço do catálogo ou só a descrição viram Servico.
        /// </summary>
        internal async Task<ResponseDto<ItemEnvio>> MontarAsync(
            int? estoqueId, int? servicoOficinaId, string? descricaoServico, int quantidade, decimal valorUnitario)
        {
            var resposta = new ResponseDto<ItemEnvio>();

            if (estoqueId.HasValue)
            {
                var estoque = await _estoque.ObterPorIdAsync(estoqueId.Value.ToString());
                if (estoque.hasErrors)
                {
                    estoque.getErrors.ForEach(e => resposta.addError(e));
                    return resposta;
                }

                var tipo = estoque.getResponse.Tipo == TipoEstoque.Peca ? "Peca" : "Insumo";
                return resposta.setResponse(new ItemEnvio(
                    Descrever(estoque.getResponse.Nome, descricaoServico), tipo, quantidade, valorUnitario, estoqueId, null));
            }

            if (servicoOficinaId.HasValue)
            {
                var servico = await _servicos.ObterPorIdAsync(servicoOficinaId.Value.ToString());
                if (servico.hasErrors)
                {
                    servico.getErrors.ForEach(e => resposta.addError(e));
                    return resposta;
                }

                return resposta.setResponse(new ItemEnvio(
                    Descrever(servico.getResponse.Nome, descricaoServico), "Servico", quantidade, valorUnitario, null, servicoOficinaId));
            }

            if (string.IsNullOrWhiteSpace(descricaoServico))
                return resposta.addError("Informe uma peça ou insumo do estoque, um serviço do catálogo ou a descrição do serviço.");

            return resposta.setResponse(new ItemEnvio(descricaoServico.Trim(), "Servico", quantidade, valorUnitario, null, null));
        }

        // Na edição o formulário volta com a descrição que o serviço guardou (que já começa pelo
        // nome); nesse caso ela é mantida, para não repetir o nome a cada gravação.
        private static string Descrever(string nome, string? complemento)
        {
            if (string.IsNullOrWhiteSpace(complemento))
                return nome;
            var texto = complemento.Trim();
            return texto.StartsWith(nome, StringComparison.OrdinalIgnoreCase) ? texto : $"{nome} - {texto}";
        }

        private async Task<ResponseDto<ItemOrdemServicoDto>> Item(int ordemServicoId, Func<Task<HttpResponseMessage>> chamada)
        {
            var resposta = await ChamadaServico.ExecutarAsync<ItemRemoto>(OrdemServicoHttpAppService.NomeServico, chamada);
            return OrdemServicoHttpAppService.Converter(resposta, i => Mapear(ordemServicoId, i));
        }

        private static ItemOrdemServicoDto Mapear(int ordemServicoId, ItemRemoto i) => new()
        {
            Id = i.Id,
            OrdemServicoId = ordemServicoId,
            EstoqueId = i.EstoqueId,
            ServicoOficinaId = i.ServicoOficinaId,
            DescricaoServico = i.Descricao,
            Quantidade = i.Quantidade,
            ValorUnitario = i.ValorUnitario,
            ValorTotal = i.ValorTotal
        };

        internal sealed record ItemEnvio(
            string Descricao, string Tipo, int Quantidade, decimal ValorUnitario, int? EstoqueId, int? ServicoOficinaId);

        private sealed record ItemRemoto(
            int Id, string Descricao, string Tipo, int Quantidade, decimal ValorUnitario, decimal ValorTotal,
            int? EstoqueId, int? ServicoOficinaId);
    }
}
