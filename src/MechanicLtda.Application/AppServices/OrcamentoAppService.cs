using AutoMapper;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Services;

namespace MechanicLtda.Application.AppServices
{
    public class OrcamentoAppService : IOrcamentoAppService
    {
        private readonly IOrcamentoService    _orcamentoService;
        private readonly IOrcamentoPdfAppService _pdfService;
        private readonly IMapper              _mapper;

        public OrcamentoAppService(
                                    IOrcamentoService orcamentoService, 
                                    IOrcamentoPdfAppService pdfService,
                                    IMapper mapper
                                    )
        {
            _orcamentoService = orcamentoService;
            _pdfService       = pdfService;
            _mapper           = mapper;
        }

        public async Task<ResponseDto<OrcamentoDto>> ObterPorIdAsync(string id)
        {
            var response = new ResponseDto<OrcamentoDto>();
            try
            {
                var orcamento = await _orcamentoService.ObterPorIdAsync(id)
                    ?? throw new KeyNotFoundException($"Orçamento com Id '{id}' não encontrado.");

                return response.setResponse(_mapper.Map<OrcamentoDto>(orcamento));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex)            { return response.addError(ex); }
        }

        public async Task<ResponseDto<OrcamentoDto>> ObterPorOrdemServicoIdAsync(int ordemServicoId)
        {
            var response = new ResponseDto<OrcamentoDto>();
            try
            {
                var orcamento = await _orcamentoService.ObterPorOrdemServicoIdAsync(ordemServicoId)
                    ?? throw new KeyNotFoundException($"Nenhum orçamento encontrado para a OS '{ordemServicoId}'.");

                return response.setResponse(_mapper.Map<OrcamentoDto>(orcamento));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex)            { return response.addError(ex); }
        }

        public async Task<ResponseDto<OrcamentoDto>> AtualizarManualAsync(string id, OrcamentoUpdateDto dto)
        {
            var response = new ResponseDto<OrcamentoDto>();
            try
            {
                var orcamento    = _mapper.Map<Orcamento>(dto);
                orcamento.Id     = int.Parse(id);
                var resultado    = await _orcamentoService.AtualizarManualAsync(orcamento);
                return response.setResponse(_mapper.Map<OrcamentoDto>(resultado));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex)            { return response.addError(ex); }
        }

        public async Task<ResponseDto<bool>> RemoverAsync(string id)
        {
            var response = new ResponseDto<bool>();
            try
            {
                await _orcamentoService.RemoverAsync(id);
                return response.setResponse(true);
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex)            { return response.addError(ex); }
        }

        public async Task<ResponseDto<byte[]>> ExportarPdfAsync(int id)
        {
            var response = new ResponseDto<byte[]>();
            try
            {
                var orcamento = await _orcamentoService.ObterComDetalhesAsync(id)
                    ?? throw new KeyNotFoundException($"Orçamento com Id '{id}' não encontrado.");

                var dto = new OrcamentoPdfDto
                {
                    Id = orcamento.Id,
                    OrdemServicoId = orcamento.OrdemServicoId,
                    DataGeracao = orcamento.DataGeracao,
                    Validade = orcamento.Validade,
                    ValorTotalPecas = orcamento.ValorTotalPecas,
                    ValorTotalInsumos = orcamento.ValorTotalInsumos,
                    ValorTotalGeral = orcamento.ValorTotalGeral,
                    DescricaoProblema = orcamento.OrdemServico.DescricaoProblema,
                    StatusOrdemServico = orcamento.OrdemServico.Status.ToString(),
                    ClienteNome = orcamento.OrdemServico.Cliente.Nome,
                    ClienteEmail = orcamento.OrdemServico.Cliente.Email,
                    ClienteTelefone = orcamento.OrdemServico.Cliente.Telefone,
                    VeiculoMarca = orcamento.OrdemServico.Veiculo.Marca,
                    VeiculoModelo = orcamento.OrdemServico.Veiculo.Modelo,
                    VeiculoPlaca = orcamento.OrdemServico.Veiculo.Placa,
                    VeiculoAno = orcamento.OrdemServico.Veiculo.Ano
                };

                var bytes = _pdfService.Gerar(dto);
                return response.setResponse(bytes);
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }
    }
}