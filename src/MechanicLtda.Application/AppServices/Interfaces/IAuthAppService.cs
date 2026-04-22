using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Enums;

namespace MechanicLtda.Application.AppServices.Interfaces
{
    public interface IAuthAppService
    {
        Task<ResponseDto<TokenDto>> LoginAsync(string email, string senha);
        Task<ResponseDto<UsuarioDto>> RegistrarAsync(string userName, string email, string senha, TipoUsuario tipo);
        Task<ResponseDto<bool>> AlterarSenhaAsync(string email, string senhaAtual, string novaSenha);
    }
}
