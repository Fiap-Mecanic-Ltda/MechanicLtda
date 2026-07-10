using AutoMapper;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Services;

namespace MechanicLtda.Application.AppServices
{
    public class OrdemServicoAppService : IOrdemServicoAppService
    {
        private readonly IOrdemServicoService _ordemServicoService;
        private readonly IMapper _mapper;

        public OrdemServicoAppService(IOrdemServicoService ordemServicoService, IMapper mapper)
        {
            _ordemServicoService = ordemServicoService;
            _mapper = mapper;
        }

        public async Task<ResponseDto<OrdemServicoDto>> AdicionarAsync(OrdemServicoCreateDto dto)
        {
            var response = new ResponseDto<OrdemServicoDto>();
            try
            {
                var ordemServico = await _ordemServicoService.AdicionarAsync(
                    dto.DescricaoProblema,
                    dto.ValorTotalEstimado,
                    dto.VeiculoId,
                    dto.ClienteId);

                return response.setResponse(_mapper.Map<OrdemServicoDto>(ordemServico));
            }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<OrdemServicoDto>> AtualizarAsync(string id, OrdemServicoUpdateDto dto)
        {
            var response = new ResponseDto<OrdemServicoDto>();
            try
            {
                var ordemServico = _mapper.Map<OrdemServico>(dto);
                ordemServico.Id = int.Parse(id);
                var resultado = await _ordemServicoService.AtualizarAsync(ordemServico);
                return response.setResponse(_mapper.Map<OrdemServicoDto>(resultado));
            }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<OrdemServicoDto>> IniciarDiagnosticoAsync(string id)
        {
            var response = new ResponseDto<OrdemServicoDto>();
            try
            {
                if (!int.TryParse(id, out var ordemId))
                    return response.addError("Id inv�lido.");

                var resultado = await _ordemServicoService.IniciarDiagnosticoAsync(ordemId);
                return response.setResponse(_mapper.Map<OrdemServicoDto>(resultado));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (InvalidOperationException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<OrdemServicoDto>> AguardarAprovacaoAsync(string id)
        {
            var response = new ResponseDto<OrdemServicoDto>();
            try
            {
                if (!int.TryParse(id, out var ordemId))
                    return response.addError("Id inv�lido.");

                var resultado = await _ordemServicoService.AguardarAprovacaoAsync(ordemId);
                return response.setResponse(_mapper.Map<OrdemServicoDto>(resultado));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (InvalidOperationException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<OrdemServicoDto>> AprovarAsync(string id)
        {
            var response = new ResponseDto<OrdemServicoDto>();
            try
            {
                if (!int.TryParse(id, out var ordemId))
                    return response.addError("Id inv�lido.");

                var resultado = await _ordemServicoService.AprovarAsync(ordemId);
                return response.setResponse(_mapper.Map<OrdemServicoDto>(resultado));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (InvalidOperationException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<OrdemServicoDto>> RecusarAsync(string id, string motivoRecusa)
        {
            var response = new ResponseDto<OrdemServicoDto>();
            try
            {
                if (!int.TryParse(id, out var ordemId))
                    return response.addError("Id inv�lido.");

                var resultado = await _ordemServicoService.RecusarAsync(ordemId, motivoRecusa);
                return response.setResponse(_mapper.Map<OrdemServicoDto>(resultado));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (InvalidOperationException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<OrdemServicoDto>> ConfirmarAprovacaoPorTokenAsync(string token, bool aprovado, string? motivoRecusa)
        {
            var response = new ResponseDto<OrdemServicoDto>();
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return response.addError("Token inv�lido.");

                var resultado = await _ordemServicoService.ConfirmarAprovacaoPorTokenAsync(token, aprovado, motivoRecusa);
                return response.setResponse(_mapper.Map<OrdemServicoDto>(resultado));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (InvalidOperationException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<OrdemServicoDto>> IniciarExecucaoAsync(string id)
        {
            var response = new ResponseDto<OrdemServicoDto>();
            try
            {
                if (!int.TryParse(id, out var ordemId))
                    return response.addError("Id inv�lido.");

                var resultado = await _ordemServicoService.IniciarExecucaoAsync(ordemId);
                return response.setResponse(_mapper.Map<OrdemServicoDto>(resultado));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (InvalidOperationException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<OrdemServicoDto>> FinalizarAsync(string id)
        {
            var response = new ResponseDto<OrdemServicoDto>();
            try
            {
                if (!int.TryParse(id, out var ordemId))
                    return response.addError("Id inv�lido.");

                var resultado = await _ordemServicoService.FinalizarAsync(ordemId);
                return response.setResponse(_mapper.Map<OrdemServicoDto>(resultado));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (InvalidOperationException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<OrdemServicoDto>> EntregarAsync(string id)
        {
            var response = new ResponseDto<OrdemServicoDto>();
            try
            {
                if (!int.TryParse(id, out var ordemId))
                    return response.addError("Id inv�lido.");

                var resultado = await _ordemServicoService.EntregarAsync(ordemId);
                return response.setResponse(_mapper.Map<OrdemServicoDto>(resultado));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (InvalidOperationException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<IEnumerable<OrdemServicoDto>>> ObterTodosAsync()
        {
            var response = new ResponseDto<IEnumerable<OrdemServicoDto>>();
            try
            {
                var ordens = await _ordemServicoService.ObterTodosAsync();
                return response.setResponse(_mapper.Map<IEnumerable<OrdemServicoDto>>(ordens));
            }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<IEnumerable<OrdemServicoDto>>> ObterPorClienteIdAsync(string clienteId)
        {
            var response = new ResponseDto<IEnumerable<OrdemServicoDto>>();
            try
            {
                var ordens = await _ordemServicoService.ObterPorClienteIdAsync(clienteId);
                return response.setResponse(_mapper.Map<IEnumerable<OrdemServicoDto>>(ordens));
            }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<IEnumerable<OrdemServicoDto>>> ObterPorStatusAsync(string statusDescricao)
        {
            var response = new ResponseDto<IEnumerable<OrdemServicoDto>>();
            try
            {
                var ordens = await _ordemServicoService.ObterPorStatusAsync(statusDescricao);
                return response.setResponse(_mapper.Map<IEnumerable<OrdemServicoDto>>(ordens));
            }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<OrdemServicoDto>> ObterPorIdAsync(string id)
        {
            var response = new ResponseDto<OrdemServicoDto>();
            try
            {
                var ordemServico = await _ordemServicoService.ObterPorIdAsync(id)
                    ?? throw new KeyNotFoundException($"Ordem de Servi�o com Id '{id}' n�o encontrada.");
                return response.setResponse(_mapper.Map<OrdemServicoDto>(ordemServico));
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<bool>> RemoverAsync(string id)
        {
            var response = new ResponseDto<bool>();
            try
            {
                await _ordemServicoService.RemoverAsync(id);
                return response.setResponse(true);
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }
    }
}