using AutoMapper;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Services;

namespace MechanicLtda.Application.AppServices
{
    public class VeiculoAppService : IVeiculoAppService
    {
        private readonly IVeiculoService _veiculoService;
        private readonly IMapper _mapper;

        public VeiculoAppService(IVeiculoService veiculoService, IMapper mapper)
        {
            _veiculoService = veiculoService;
            _mapper = mapper;
        }

        public async Task<ResponseDto<VeiculoDto>> AdicionarAsync(VeiculoCreateDto dto)
        {
            var response = new ResponseDto<VeiculoDto>();
            try
            {
                var veiculo = await _veiculoService.AdicionarAsync(dto.Placa, dto.Marca, dto.Modelo, dto.Ano, dto.ClienteId);
                return response.setResponse(_mapper.Map<VeiculoDto>(veiculo));
            }
            catch (Exception ex)
            {
                return response.addError(ex);
            }
        }

        public async Task<ResponseDto<VeiculoDto>> AtualizarAsync(string id, VeiculoUpdateDto dto)
        {
            var response = new ResponseDto<VeiculoDto>();
            try
            {
                var veiculo = _mapper.Map<Veiculo>(dto);
                veiculo.Id = int.Parse(id);
                var resultado = await _veiculoService.AtualizarAsync(veiculo);
                return response.setResponse(_mapper.Map<VeiculoDto>(resultado));
            }
            catch (Exception ex)
            {
                return response.addError(ex);
            }
        }

        public async Task<ResponseDto<IEnumerable<VeiculoDto>>> ObterTodosAsync()
        {
            var response = new ResponseDto<IEnumerable<VeiculoDto>>();
            try
            {
                var veiculos = await _veiculoService.ObterTodosAsync();
                return response.setResponse(_mapper.Map<IEnumerable<VeiculoDto>>(veiculos));
            }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<IEnumerable<VeiculoDto>>> ObterPorClienteIdAsync(string clienteId)
        {
            var response = new ResponseDto<IEnumerable<VeiculoDto>>();
            try
            {
                var veiculos = await _veiculoService.ObterPorClienteIdAsync(clienteId);
                return response.setResponse(_mapper.Map<IEnumerable<VeiculoDto>>(veiculos));
            }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<VeiculoDto>> ObterPorIdAsync(string id)
        {
            var response = new ResponseDto<VeiculoDto>();
            try
            {
                var veiculo = await _veiculoService.ObterPorIdAsync(id)
                    ?? throw new KeyNotFoundException($"Veículo com Id '{id}' não encontrado.");
                return response.setResponse(_mapper.Map<VeiculoDto>(veiculo));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<bool>> RemoverAsync(string id)
        {
            var response = new ResponseDto<bool>();
            try
            {
                await _veiculoService.RemoverAsync(id);
                return response.setResponse(true);
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }
    }
}