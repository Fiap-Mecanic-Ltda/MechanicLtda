using AutoMapper;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;
using MechanicLtda.Domain.Interfaces.Services;

namespace MechanicLtda.Application.AppServices
{
    public class EstoqueAppService : IEstoqueAppService
    {
        private readonly IEstoqueService _estoqueService;
        private readonly IMapper _mapper;

        public EstoqueAppService(IEstoqueService estoqueService, IMapper mapper)
        {
            _estoqueService = estoqueService;
            _mapper         = mapper;
        }

        public async Task<ResponseDto<EstoqueDto>> AdicionarAsync(EstoqueCreateDto dto)
        {
            var response = new ResponseDto<EstoqueDto>();
            try
            {
                var entidade = _mapper.Map<Estoque>(dto);
                var resultado = await _estoqueService.AdicionarAsync(entidade);
                return response.setResponse(MapToDto(resultado));
            }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<EstoqueDto>> AtualizarAsync(string id, EstoqueUpdateDto dto)
        {
            var response = new ResponseDto<EstoqueDto>();
            try
            {
                var entidade = _mapper.Map<Estoque>(dto);
                entidade.Id = int.Parse(id);
                var resultado = await _estoqueService.AtualizarAsync(entidade);
                return response.setResponse(MapToDto(resultado));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<bool>> RemoverAsync(string id)
        {
            var response = new ResponseDto<bool>();
            try
            {
                await _estoqueService.RemoverAsync(id);
                return response.setResponse(true);
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<IEnumerable<EstoqueDto>>> ObterTodosAsync()
        {
            var response = new ResponseDto<IEnumerable<EstoqueDto>>();
            try
            {
                var lista = await _estoqueService.ObterTodosAsync();
                return response.setResponse(lista.Select(MapToDto));
            }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<IEnumerable<EstoqueDto>>> ObterPorTipoAsync(TipoEstoque tipo)
        {
            var response = new ResponseDto<IEnumerable<EstoqueDto>>();
            try
            {
                var lista = await _estoqueService.ObterPorTipoAsync(tipo);
                return response.setResponse(lista.Select(MapToDto));
            }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<EstoqueDto>> ObterPorIdAsync(string id)
        {
            var response = new ResponseDto<EstoqueDto>();
            try
            {
                var estoque = await _estoqueService.ObterPorIdAsync(id)
                    ?? throw new KeyNotFoundException($"Estoque com Id '{id}' não encontrado.");
                return response.setResponse(MapToDto(estoque));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<EstoqueDto>> ReporQuantidadeAsync(string id, EstoqueReposicaoDto dto)
        {
            var response = new ResponseDto<EstoqueDto>();
            try
            {
                var resultado = await _estoqueService.ReporQuantidadeAsync(int.Parse(id), dto.QuantidadeEntrada);
                return response.setResponse(MapToDto(resultado));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (ArgumentException ex)    { return response.addError(ex.Message); }
            catch (Exception ex)            { return response.addError(ex); }
        }

        // Mapeamento manual para incluir a propriedade calculada BaixoEstoque
        private static EstoqueDto MapToDto(Estoque e) => new()
        {
            Id                   = e.Id,
            Nome                 = e.Nome,
            Tipo                 = e.Tipo,
            QuantidadeAtual      = e.QuantidadeAtual,
            QuantidadeMinima     = e.QuantidadeMinima,
            DataUltimaAtualizacao = e.DataUltimaAtualizacao,
            BaixoEstoque         = e.EstaBaixoEstoque()
        };
    }
}