using MechanicLtda.Application.DTOs;

namespace MechanicLtda.Application.AppServices.Interfaces
{
    public interface IItemOrdemServicoAppService
    {
        Task<ResponseDto<ItemOrdemServicoDto>> AdicionarAsync(int ordemServicoId, ItemOrdemServicoCreateDto dto);
        Task<ResponseDto<ItemOrdemServicoDto>> AtualizarAsync(string id, ItemOrdemServicoUpdateDto dto);
        Task<ResponseDto<bool>> RemoverAsync(string id);
        Task<ResponseDto<IEnumerable<ItemOrdemServicoDto>>> ObterPorOrdemServicoIdAsync(int ordemServicoId);
        Task<ResponseDto<ItemOrdemServicoDto>> ObterPorIdAsync(string id);
    }
}