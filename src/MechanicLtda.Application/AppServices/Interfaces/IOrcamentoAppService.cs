using MechanicLtda.Application.DTOs;

namespace MechanicLtda.Application.AppServices.Interfaces
{
    public interface IOrcamentoAppService
    {
        Task<ResponseDto<OrcamentoDto>> ObterPorIdAsync(string id);
        Task<ResponseDto<OrcamentoDto>> ObterPorOrdemServicoIdAsync(int ordemServicoId);
        Task<ResponseDto<OrcamentoDto>> AtualizarManualAsync(string id, OrcamentoUpdateDto dto);
        Task<ResponseDto<bool>> RemoverAsync(string id);
    }
}