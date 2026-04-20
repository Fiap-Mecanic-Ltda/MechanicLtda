using MechanicLtda.Application.DTOs;

namespace MechanicLtda.Application.AppServices.Interfaces
{
    public interface IUsuarioAppService
    {
        Task<ResponseDto<UsuarioDto>> AdicionarAsync(UsuarioCreateDto dto);
        Task<ResponseDto<UsuarioDto>> AtualizarAsync(string id, UsuarioUpdateDto dto);
        Task<ResponseDto<IEnumerable<UsuarioDto>>> ObterTodosAsync();
        Task<ResponseDto<bool>> RemoverAsync(string id);
    }
}
