using AutoMapper;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Services;

namespace MechanicLtda.Application.AppServices
{
    public class ItemOrdemServicoAppService : IItemOrdemServicoAppService
    {
        private readonly IItemOrdemServicoService _itemService;
        private readonly IMapper _mapper;

        public ItemOrdemServicoAppService(IItemOrdemServicoService itemService, IMapper mapper)
        {
            _itemService = itemService;
            _mapper = mapper;
        }

        public async Task<ResponseDto<ItemOrdemServicoDto>> AdicionarAsync(int ordemServicoId, ItemOrdemServicoCreateDto dto)
        {
            var response = new ResponseDto<ItemOrdemServicoDto>();
            try
            {
                var item = await _itemService.AdicionarAsync(
                    ordemServicoId,
                    dto.EstoqueId,
                    dto.Quantidade,
                    dto.ValorUnitario);

                return response.setResponse(_mapper.Map<ItemOrdemServicoDto>(item));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<ItemOrdemServicoDto>> AtualizarAsync(string id, ItemOrdemServicoUpdateDto dto)
        {
            var response = new ResponseDto<ItemOrdemServicoDto>();
            try
            {
                var item = _mapper.Map<ItemOrdemServico>(dto);
                item.Id = int.Parse(id);
                var resultado = await _itemService.AtualizarAsync(item);
                return response.setResponse(_mapper.Map<ItemOrdemServicoDto>(resultado));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<bool>> RemoverAsync(string id)
        {
            var response = new ResponseDto<bool>();
            try
            {
                await _itemService.RemoverAsync(id);
                return response.setResponse(true);
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<IEnumerable<ItemOrdemServicoDto>>> ObterPorOrdemServicoIdAsync(int ordemServicoId)
        {
            var response = new ResponseDto<IEnumerable<ItemOrdemServicoDto>>();
            try
            {
                var itens = await _itemService.ObterPorOrdemServicoIdAsync(ordemServicoId);
                return response.setResponse(_mapper.Map<IEnumerable<ItemOrdemServicoDto>>(itens));
            }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<ItemOrdemServicoDto>> ObterPorIdAsync(string id)
        {
            var response = new ResponseDto<ItemOrdemServicoDto>();
            try
            {
                var item = await _itemService.ObterPorIdAsync(id)
                    ?? throw new KeyNotFoundException($"Item com Id '{id}' não encontrado.");
                return response.setResponse(_mapper.Map<ItemOrdemServicoDto>(item));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }
    }
}