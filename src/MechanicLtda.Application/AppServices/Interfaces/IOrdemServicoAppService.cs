using MechanicLtda.Application.DTOs;

namespace MechanicLtda.Application.AppServices.Interfaces
{
    public interface IOrdemServicoAppService
    {
        Task<ResponseDto<OrdemServicoDto>> AdicionarAsync(OrdemServicoCreateDto dto);
        Task<ResponseDto<OrdemServicoDto>> AtualizarAsync(string id, OrdemServicoUpdateDto dto);
        Task<ResponseDto<OrdemServicoDto>> IniciarDiagnosticoAsync(string id);
        Task<ResponseDto<OrdemServicoDto>> AguardarAprovacaoAsync(string id);
        Task<ResponseDto<OrdemServicoDto>> AprovarAsync(string id);
        Task<ResponseDto<OrdemServicoDto>> RecusarAsync(string id, string motivoRecusa);
        Task<ResponseDto<OrdemServicoDto>> ConfirmarAprovacaoPorTokenAsync(string token, bool aprovado, string? motivoRecusa);
        Task<ResponseDto<OrdemServicoDto>> IniciarExecucaoAsync(string id);
        Task<ResponseDto<OrdemServicoDto>> FinalizarAsync(string id);
        Task<ResponseDto<OrdemServicoDto>> EntregarAsync(string id);
        Task<ResponseDto<IEnumerable<OrdemServicoDto>>> ObterTodosAsync();
        Task<ResponseDto<TempoMedioExecucaoDto>> ObterTempoMedioExecucaoAsync();
        Task<ResponseDto<IEnumerable<OrdemServicoDto>>> ObterPorClienteIdAsync(string clienteId);
        Task<ResponseDto<IEnumerable<OrdemServicoDto>>> ObterPorStatusAsync(string statusDescricao);
        Task<ResponseDto<OrdemServicoDto>> ObterPorIdAsync(string id);
        Task<ResponseDto<bool>> RemoverAsync(string id);
    }
}
