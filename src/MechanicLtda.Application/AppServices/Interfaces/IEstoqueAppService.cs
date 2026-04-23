using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Enums;

namespace MechanicLtda.Application.AppServices.Interfaces
{
    public interface IEstoqueAppService
    {
        Task<ResponseDto<EstoqueDto>> AdicionarAsync(EstoqueCreateDto dto);
        Task<ResponseDto<EstoqueDto>> AtualizarAsync(string id, EstoqueUpdateDto dto);
        Task<ResponseDto<bool>> RemoverAsync(string id);
        Task<ResponseDto<IEnumerable<EstoqueDto>>> ObterTodosAsync();
        Task<ResponseDto<IEnumerable<EstoqueDto>>> ObterPorTipoAsync(TipoEstoque tipo);
        Task<ResponseDto<EstoqueDto>> ObterPorIdAsync(string id);
        Task<ResponseDto<EstoqueDto>> ReporQuantidadeAsync(string id, EstoqueReposicaoDto dto);
    }
}