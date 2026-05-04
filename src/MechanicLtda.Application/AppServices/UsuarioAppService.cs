using AutoMapper;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;
using MechanicLtda.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Identity;

namespace MechanicLtda.Application.AppServices
{
    public class UsuarioAppService : IUsuarioAppService
    {
        private readonly IUsuarioService    _usuarioService;
        private readonly UserManager<Usuario> _userManager;
        private readonly IMapper            _mapper;

        public UsuarioAppService(IUsuarioService      usuarioService,
                                 UserManager<Usuario> userManager,
                                 IMapper              mapper)
        {
            _usuarioService = usuarioService;
            _userManager    = userManager;
            _mapper         = mapper;
        }

        public async Task<ResponseDto<UsuarioDto>> AdicionarAsync(UsuarioCreateDto dto)
        {
            var response = new ResponseDto<UsuarioDto>();

            try
            {
                var usuario = await _usuarioService.AdicionarAsnyc(dto.UserName, dto.Email, dto.Tipo);

                // Atribui o role correspondente ao tipo informado
                var role = ObterRolePorTipo(dto.Tipo);
                await _userManager.AddToRoleAsync(usuario, role);

                return response.setResponse(_mapper.Map<UsuarioDto>(usuario));
            }
            catch (Exception ex)
            {
                return response.addError(ex);
            }
        }

        public async Task<ResponseDto<UsuarioDto>> AtualizarAsync(string id, UsuarioUpdateDto dto)
        {
            var response = new ResponseDto<UsuarioDto>();

            try
            {
                var usuario = _mapper.Map<Usuario>(dto);
                usuario.Id  = id;

                var resultado = await _usuarioService.AtualizarAsync(usuario);

                // Atualiza o role caso o tipo tenha sido alterado
                await AtualizarRoleAsync(resultado, dto.Tipo);

                return response.setResponse(_mapper.Map<UsuarioDto>(resultado));
            }
            catch (Exception ex)
            {
                return response.addError(ex);
            }
        }

        public async Task<ResponseDto<IEnumerable<UsuarioDto>>> ObterTodosAsync()
        {
            var response = new ResponseDto<IEnumerable<UsuarioDto>>();
            try
            {
                var usuarios = await _usuarioService.ObterTodosAsync();
                return response.setResponse(_mapper.Map<IEnumerable<UsuarioDto>>(usuarios));
            }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<bool>> RemoverAsync(string id)
        {
            var response = new ResponseDto<bool>();
            try
            {
                await _usuarioService.RemoverAsync(id);
                return response.setResponse(true);
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex)            { return response.addError(ex); }
        }

        // ─── Privado ────────────────────────────────────────────────────────────

        /// <summary>
        /// Remove todos os roles atuais do usuário e atribui o novo role
        /// correspondente ao tipo informado.
        /// </summary>
        private async Task AtualizarRoleAsync(Usuario usuario, TipoUsuario novoTipo)
        {
            var rolesAtuais = await _userManager.GetRolesAsync(usuario);

            if (rolesAtuais.Any())
                await _userManager.RemoveFromRolesAsync(usuario, rolesAtuais);

            await _userManager.AddToRoleAsync(usuario, ObterRolePorTipo(novoTipo));
        }

        private static string ObterRolePorTipo(TipoUsuario tipo) => tipo switch
        {
            TipoUsuario.Administrador => "Administrador",
            TipoUsuario.Funcionario   => "Funcionario",
            TipoUsuario.Cliente       => "Cliente",
            _                         => "Cliente"
        };
    }
}
