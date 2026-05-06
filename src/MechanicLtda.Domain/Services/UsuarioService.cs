using AutoMapper;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Domain.Interfaces.Services;
using MechanicLtda.Domain.Services.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MechanicLtda.Domain.Services
{
    public class UsuarioService : BaseService, IUsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ILogger<UsuarioService> _logger;
        private readonly IMapper _mapper;

        public UsuarioService(ILogger<UsuarioService> logger, 
                              IConfiguration configuration,
                              INotificadorService notificadorService,
                              IUsuarioRepository usuarioRepository) : base(notificadorService, configuration)
        {
            _usuarioRepository = usuarioRepository;
            _logger = logger;
        }

        public async Task<Usuario> AdicionarAsnyc(string userName, string email, TipoUsuario tipo)
        {
            try
            {
                bool emailExiste = await _usuarioRepository.EmailExisteAsync(email);
                if (emailExiste)
                    throw new InvalidOperationException($"Já existe um usuário com o e-mail '{email}'.");

                var usuario = new Usuario
                {
                    UserName = userName,
                    Email = email,
                    Tipo = tipo,
                    Ativo = true,
                    DataCriacao = DateTime.Now
                };

                return await _usuarioRepository.AdicionarAsync(usuario);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo UsuarioService:AdicionarAsnyc", _logger);
                throw ex;
            }
        }

        public async Task<Usuario> AtualizarAsync(Usuario usuario)
        {
            try
            {
                // Carrega a entidade já rastreada pelo EF Core
                var existente = await _usuarioRepository.ObterPorIdAsync(usuario.Id)
                        ?? throw new KeyNotFoundException($"Usuário com Id '{usuario.Id}' não encontrado.");

                var emailEmUso = await _usuarioRepository.ObterPorEmailAsync(usuario.Email);
                if (emailEmUso is not null && emailEmUso.Id != usuario.Id)
                    throw new InvalidOperationException($"Já existe um usuário com o e-mail '{usuario.Email}'.");

                // Atualiza as propriedades da instância rastreada para evitar
                // conflito de tracking ao chamar _dbSet.Update com outra instância
                existente.UserName        = usuario.UserName;
                existente.Email           = usuario.Email;
                existente.Tipo            = usuario.Tipo;
                existente.Ativo           = usuario.Ativo;
                existente.DataModificacao = DateTime.UtcNow;

                return await _usuarioRepository.AtualizarAsync(existente);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo UsuarioService:AtualizarAsync", _logger);
                throw ex;
            }
        }

        public async Task<IEnumerable<Usuario>> ObterTodosAsync()
        {
            try
            {
                return await _usuarioRepository.ObterTodosAsync();
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo UsuarioService:ObterTodosAsnyc", _logger);
                throw ex;
            }
        }

        public async Task RemoverAsync(string id)
        {
            try
            {
                _ = await _usuarioRepository.ObterPorIdAsync(id)
                     ?? throw new KeyNotFoundException($"Usuário com Id '{id}' não encontrado.");

                await _usuarioRepository.RemoverAsync(id);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo UsuarioService:RemoverAsync", _logger);
                throw ex;
            }
        }
    }
}