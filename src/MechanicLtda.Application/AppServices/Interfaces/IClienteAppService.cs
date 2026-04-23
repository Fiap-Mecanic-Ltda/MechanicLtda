using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MechanicLtda.Application.DTOs;

namespace MechanicLtda.Application.AppServices.Interfaces
{
    public interface IClienteAppService
    {
        Task<ResponseDto<ClienteDto>> AdicionarAsync(ClienteCreateDto dto);
        Task<ResponseDto<ClienteDto>> AtualizarAsync(string id, ClienteUpdateDto dto);
        Task<ResponseDto<IEnumerable<ClienteDto>>> ObterTodosAsync();
        Task<ResponseDto<ClienteDto>> ObterPorIdAsync(string id);
        Task<ResponseDto<bool>> RemoverAsync(string id);
    }
}
