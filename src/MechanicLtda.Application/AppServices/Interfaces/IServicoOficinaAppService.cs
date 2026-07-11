using MechanicLtda.Application.DTOs;

namespace MechanicLtda.Application.AppServices.Interfaces
{
    public interface IServicoOficinaAppService
    {
        Task<ResponseDto<ServicoOficinaDto>> AdicionarAsync(ServicoOficinaCreateDto dto);
        Task<ResponseDto<ServicoOficinaDto>> AtualizarAsync(string id, ServicoOficinaUpdateDto dto);
        Task<ResponseDto<bool>> RemoverAsync(string id);
        Task<ResponseDto<IEnumerable<ServicoOficinaDto>>> ObterTodosAsync();
        Task<ResponseDto<IEnumerable<ServicoOficinaDto>>> ObterAtivosAsync();
        Task<ResponseDto<ServicoOficinaDto>> ObterPorIdAsync(string id);
    }
}
