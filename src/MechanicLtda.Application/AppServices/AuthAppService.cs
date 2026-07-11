using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Application.Security;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MechanicLtda.Application.AppServices
{
    public class AuthAppService : IAuthAppService
    {
        private readonly UserManager<Usuario>   _userManager;
        private readonly SignInManager<Usuario> _signInManager;
        private readonly IConfiguration        _configuration;

        public AuthAppService(UserManager<Usuario>   userManager,
                              SignInManager<Usuario> signInManager,
                              IConfiguration        configuration)
        {
            _userManager   = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        public async Task<ResponseDto<TokenDto>> LoginAsync(string email, string senha)
        {
            var response = new ResponseDto<TokenDto>();

            try
            {
                var usuario = await _userManager.FindByEmailAsync(email);
                if (usuario is null || !usuario.Ativo)
                    return response.addError("Credenciais inválidas.");

                var resultado = await _signInManager.CheckPasswordSignInAsync(usuario, senha, lockoutOnFailure: false);
                if (!resultado.Succeeded)
                    return response.addError("Credenciais inválidas.");

                var token = await GerarTokenAsync(usuario);
                return response.setResponse(token);
            }
            catch (Exception ex)
            {
                return response.addError(ex);
            }
        }

        public async Task<ResponseDto<UsuarioDto>> RegistrarAsync(string userName, string email, string senha, TipoUsuario tipo)
        {
            var response = new ResponseDto<UsuarioDto>();

            try
            {
                var usuarioExistente = await _userManager.FindByEmailAsync(email);
                if (usuarioExistente is not null)
                    return response.addError($"Já existe um usuário com o e-mail '{email}'.");

                var usuario = new Usuario
                {
                    UserName    = userName,
                    Email       = email,
                    Tipo        = tipo,
                    Ativo       = true,
                    DataCriacao = DateTime.UtcNow
                };

                var resultado = await _userManager.CreateAsync(usuario, senha);
                if (!resultado.Succeeded)
                {
                    foreach (var erro in resultado.Errors)
                        response.addError(erro.Description);

                    return response;
                }

                // Atribui o role correspondente ao tipo de usuário
                var role = ObterRolePorTipo(tipo);
                await _userManager.AddToRoleAsync(usuario, role);

                return response.setResponse(new UsuarioDto
                {
                    Id       = Guid.Parse(usuario.Id),
                    UserName = usuario.UserName!,
                    Email    = usuario.Email!,
                    Ativo    = usuario.Ativo
                });
            }
            catch (Exception ex)
            {
                return response.addError(ex);
            }
        }

        public async Task<ResponseDto<bool>> AlterarSenhaAsync(string email, string senhaAtual, string novaSenha)
        {
            var response = new ResponseDto<bool>();

            try
            {
                var usuario = await _userManager.FindByEmailAsync(email);
                if (usuario is null || !usuario.Ativo)
                    return response.addError("Usuário não encontrado.");

                var resultado = await _userManager.ChangePasswordAsync(usuario, senhaAtual, novaSenha);
                if (!resultado.Succeeded)
                {
                    foreach (var erro in resultado.Errors)
                        response.addError(erro.Description);

                    return response;
                }

                return response.setResponse(true);
            }
            catch (Exception ex)
            {
                return response.addError(ex);
            }
        }

        // ─── Privado ────────────────────────────────────────────────────────────

        private static string ObterRolePorTipo(TipoUsuario tipo) => tipo switch
        {
            TipoUsuario.Administrador => "Administrador",
            TipoUsuario.Funcionario   => "Funcionario",
            TipoUsuario.Cliente       => "Cliente",
            _                         => "Cliente"
        };

        private async Task<TokenDto> GerarTokenAsync(Usuario usuario)
        {
            var roles = await _userManager.GetRolesAsync(usuario);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub,   usuario.Id),
                new(JwtRegisteredClaimNames.Email, usuario.Email!),
                new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                new("userName", usuario.UserName!),
                new("tipo",     usuario.Tipo.ToString())
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var jwtSettings  = _configuration.GetSection("JwtSettings");
            var rawSecretKey = JwtSecretProvider.GetSecretKey(_configuration);

            var secretKey  = Encoding.UTF8.GetBytes(rawSecretKey);
            var expiracao  = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiracaoMinutos"]));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject            = new ClaimsIdentity(claims),
                Expires            = expiracao,
                Issuer             = jwtSettings["Issuer"],
                Audience           = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(secretKey),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token        = tokenHandler.CreateToken(tokenDescriptor);

            return new TokenDto
            {
                Token     = tokenHandler.WriteToken(token),
                Expiracao = expiracao
            };
        }
    }
}
