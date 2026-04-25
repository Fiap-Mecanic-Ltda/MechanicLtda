using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Domain.Interfaces.Services;
using MechanicLtda.Domain.Services.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MechanicLtda.Domain.Services
{
    public class ClienteService : BaseService, IClienteService
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly ILogger<ClienteService> _logger;

        public ClienteService(ILogger<ClienteService> logger,
                              IConfiguration configuration,
                              INotificadorService notificadorService,
                              IClienteRepository clienteRepository) : base(notificadorService, configuration)
        {
            _clienteRepository = clienteRepository;
            _logger = logger;
        }

        public async Task<Cliente> AdicionarAsync(string nome, string email, string? telefone, string cpfCnpj)
        {
            try
            {
                bool emailExiste = await _clienteRepository.EmailExisteAsync(email);
                if (emailExiste)
                    throw new InvalidOperationException($"Já existe um cliente com o e-mail '{email}'.");

                var cliente = new Cliente
                {
                    Nome = nome,
                    Email = email,
                    Telefone = telefone,
                    CpfCnpj = cpfCnpj,
                    Ativo = true,
                    DataCriacao = DateTime.Now
                };

                return await _clienteRepository.AdicionarAsync(cliente);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo ClienteService:AdicionarAsync", _logger);
                throw;
            }
        }

        public async Task<Cliente> AtualizarAsync(Cliente cliente)
        {
            try
            {
                var existente = await _clienteRepository.ObterPorIdAsync(cliente.Id.ToString())
                    ?? throw new KeyNotFoundException($"Cliente com Id '{cliente.Id}' não encontrado.");

                var emailEmUso = await _clienteRepository.ObterPorEmailAsync(cliente.Email);
                if (emailEmUso is not null && emailEmUso.Id != cliente.Id)
                    throw new InvalidOperationException($"Já existe um cliente com o e-mail '{cliente.Email}'.");

                cliente.DataCriacao = existente.DataCriacao;
                cliente.DataModificacao = DateTime.UtcNow;

                return await _clienteRepository.AtualizarAsync(cliente);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo ClienteService:AtualizarAsync", _logger);
                throw;
            }
        }

        public async Task<IEnumerable<Cliente>> ObterTodosAsync()
        {
            try
            {
                return await _clienteRepository.ObterTodosAsync();
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo ClienteService:ObterTodosAsync", _logger);
                throw;
            }
        }

        public async Task<Cliente?> ObterPorIdAsync(string id)
        {
            try
            {
                return await _clienteRepository.ObterPorIdAsync(id);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo ClienteService:ObterPorIdAsync", _logger);
                throw;
            }
        }

        public async Task RemoverAsync(string id)
        {
            try
            {
                var existente = await _clienteRepository.ObterPorIdAsync(id)
                    ?? throw new KeyNotFoundException($"Cliente com Id '{id}' não encontrado.");

                await _clienteRepository.RemoverAsync(id);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo ClienteService:RemoverAsync", _logger);
                throw;
            }
        }
    }
}
