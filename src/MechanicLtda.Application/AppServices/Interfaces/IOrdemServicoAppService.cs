using MechanicLtda.Application.DTOs;

namespace MechanicLtda.Application.AppServices.Interfaces
{
    public interface IOrdemServicoAppService
    {
        Task<ResponseDto<OrdemServicoDto>> AdicionarAsync(OrdemServicoCreateDto dto);
        Task<ResponseDto<OrdemServicoDto>> AtualizarAsync(string id, OrdemServicoUpdateDto dto);
        Task<ResponseDto<OrdemServicoDto>> IniciarDiagnosticoAsync(string id);
        Task<ResponseDto<OrdemServicoDto>> AguardarAprovacaoAsync(string id);
        Task<ResponseDto<OrdemServicoDto>> IniciarExecucaoAsync(string id);
        Task<ResponseDto<OrdemServicoDto>> FinalizarAsync(string id);
        Task<ResponseDto<OrdemServicoDto>> EntregarAsync(string id);
        Task<ResponseDto<IEnumerable<OrdemServicoDto>>> ObterTodosAsync();
        Task<ResponseDto<IEnumerable<OrdemServicoDto>>> ObterPorClienteIdAsync(string clienteId);
        Task<ResponseDto<OrdemServicoDto>> ObterPorIdAsync(string id);
        Task<ResponseDto<bool>> RemoverAsync(string id);
    }
}