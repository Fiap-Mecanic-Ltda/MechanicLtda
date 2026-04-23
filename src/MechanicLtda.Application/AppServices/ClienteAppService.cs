using AutoMapper;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Services;

namespace MechanicLtda.Application.AppServices
{
    public class ClienteAppService : IClienteAppService
    {
        private readonly IClienteService _clienteService;
        private readonly IMapper _mapper;

        public ClienteAppService(IClienteService clienteService, IMapper mapper)
        {
            _clienteService = clienteService;
            _mapper = mapper;
        }

        public async Task<ResponseDto<ClienteDto>> AdicionarAsync(ClienteCreateDto dto)
        {
            var response = new ResponseDto<ClienteDto>();
            try
            {
                var cliente = await _clienteService.AdicionarAsync(dto.Nome, dto.Email, dto.Telefone);
                return response.setResponse(_mapper.Map<ClienteDto>(cliente));
            }
            catch (Exception ex)
            {
                return response.addError(ex);
            }
        }

        public async Task<ResponseDto<ClienteDto>> AtualizarAsync(string id, ClienteUpdateDto dto)
        {
            var response = new ResponseDto<ClienteDto>();
            try
            {
                var cliente = _mapper.Map<Cliente>(dto);
                cliente.Id = Guid.Parse(id);
                var resultado = await _clienteService.AtualizarAsync(cliente);
                return response.setResponse(_mapper.Map<ClienteDto>(resultado));
            }
            catch (Exception ex)
            {
                return response.addError(ex);
            }
        }

        public async Task<ResponseDto<IEnumerable<ClienteDto>>> ObterTodosAsync()
        {
            var response = new ResponseDto<IEnumerable<ClienteDto>>();
            try
            {
                var clientes = await _clienteService.ObterTodosAsync();
                return response.setResponse(_mapper.Map<IEnumerable<ClienteDto>>(clientes));
            }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<ClienteDto>> ObterPorIdAsync(string id)
        {
            var response = new ResponseDto<ClienteDto>();
            try
            {
                var cliente = await _clienteService.ObterPorIdAsync(id)
                    ?? throw new KeyNotFoundException($"Cliente com Id '{id}' não encontrado.");
                return response.setResponse(_mapper.Map<ClienteDto>(cliente));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<bool>> RemoverAsync(string id)
        {
            var response = new ResponseDto<bool>();
            try
            {
                await _clienteService.RemoverAsync(id);
                return response.setResponse(true);
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }
    }
}
