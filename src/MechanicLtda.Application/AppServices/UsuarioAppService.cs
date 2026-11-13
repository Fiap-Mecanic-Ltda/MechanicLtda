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
                // A senha é conferida antes de gravar: um usuário sem senha nunca consegue entrar.
                var errosSenha = await ValidarSenhaAsync(dto);
                if (errosSenha.Count > 0)
                {
                    errosSenha.ForEach(e => response.addError(e));
                    return response;
                }

                var usuario = await _usuarioService.AdicionarAsnyc(dto.UserName, dto.Email, dto.Tipo);

                var senha = await _userManager.AddPasswordAsync(usuario, dto.Senha);
                if (!senha.Succeeded)
                {
                    await _usuarioService.RemoverAsync(usuario.Id);
                    foreach (var erro in senha.Errors) response.addError(erro.Description);
                    return response;
                }

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

        private async Task<List<string>> ValidarSenhaAsync(UsuarioCreateDto dto)
        {
            var erros = new List<string>();
            if (string.IsNullOrWhiteSpace(dto.Senha))
            {
                erros.Add("Informe a senha.");
                return erros;
            }

            var candidato = new Usuario { UserName = dto.UserName, Email = dto.Email };
            foreach (var validador in _userManager.PasswordValidators)
            {
                var resultado = await validador.ValidateAsync(_userManager, candidato, dto.Senha);
                erros.AddRange(resultado.Errors.Select(e => e.Description));
            }
            return erros;
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
