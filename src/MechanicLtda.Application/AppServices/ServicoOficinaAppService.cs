using AutoMapper;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Services;

namespace MechanicLtda.Application.AppServices
{
    public class ServicoOficinaAppService : IServicoOficinaAppService
    {
        private readonly IServicoOficinaService _servicoService;
        private readonly IMapper _mapper;

        public ServicoOficinaAppService(IServicoOficinaService servicoService, IMapper mapper)
        {
            _servicoService = servicoService;
            _mapper = mapper;
        }

        public async Task<ResponseDto<ServicoOficinaDto>> AdicionarAsync(ServicoOficinaCreateDto dto)
        {
            var response = new ResponseDto<ServicoOficinaDto>();
            try
            {
                var entidade = _mapper.Map<ServicoOficina>(dto);
                var resultado = await _servicoService.AdicionarAsync(entidade);
                return response.setResponse(_mapper.Map<ServicoOficinaDto>(resultado));
            }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<ServicoOficinaDto>> AtualizarAsync(string id, ServicoOficinaUpdateDto dto)
        {
            var response = new ResponseDto<ServicoOficinaDto>();
            try
            {
                var entidade = _mapper.Map<ServicoOficina>(dto);
                entidade.Id = int.Parse(id);
                var resultado = await _servicoService.AtualizarAsync(entidade);
                return response.setResponse(_mapper.Map<ServicoOficinaDto>(resultado));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<bool>> RemoverAsync(string id)
        {
            var response = new ResponseDto<bool>();
            try
            {
                await _servicoService.RemoverAsync(id);
                return response.setResponse(true);
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<IEnumerable<ServicoOficinaDto>>> ObterTodosAsync()
        {
            var response = new ResponseDto<IEnumerable<ServicoOficinaDto>>();
            try
            {
                var lista = await _servicoService.ObterTodosAsync();
                return response.setResponse(_mapper.Map<IEnumerable<ServicoOficinaDto>>(lista));
            }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<IEnumerable<ServicoOficinaDto>>> ObterAtivosAsync()
        {
            var response = new ResponseDto<IEnumerable<ServicoOficinaDto>>();
            try
            {
                var lista = await _servicoService.ObterAtivosAsync();
                return response.setResponse(_mapper.Map<IEnumerable<ServicoOficinaDto>>(lista));
            }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<ServicoOficinaDto>> ObterPorIdAsync(string id)
        {
            var response = new ResponseDto<ServicoOficinaDto>();
            try
            {
                var servico = await _servicoService.ObterPorIdAsync(id)
                    ?? throw new KeyNotFoundException($"Serviço com Id '{id}' não encontrado.");
                return response.setResponse(_mapper.Map<ServicoOficinaDto>(servico));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }
    }
}
