using MechanicLtda.Application.DTOs;

namespace MechanicLtda.Application.AppServices.Interfaces
{
    public interface IVeiculoAppService
    {
        Task<ResponseDto<VeiculoDto>> AdicionarAsync(VeiculoCreateDto dto);
        Task<ResponseDto<VeiculoDto>> AtualizarAsync(string id, VeiculoUpdateDto dto);
        Task<ResponseDto<IEnumerable<VeiculoDto>>> ObterTodosAsync();
        Task<ResponseDto<IEnumerable<VeiculoDto>>> ObterPorClienteIdAsync(string clienteId);
        Task<ResponseDto<VeiculoDto>> ObterPorIdAsync(string id);
        Task<ResponseDto<bool>> RemoverAsync(string id);
    }
}