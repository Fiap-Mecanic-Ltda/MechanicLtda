    using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Domain.Interfaces.Services;
using MechanicLtda.Domain.Services.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MechanicLtda.Domain.Services
{
    public class VeiculoService : BaseService, IVeiculoService
    {
        private readonly IVeiculoRepository _veiculoRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly ILogger<VeiculoService> _logger;

        public VeiculoService(ILogger<VeiculoService> logger,
                              IConfiguration configuration,
                              INotificadorService notificadorService,
                              IVeiculoRepository veiculoRepository,
                              IClienteRepository clienteRepository) : base(notificadorService, configuration)
        {
            _veiculoRepository = veiculoRepository;
            _clienteRepository = clienteRepository;
            _logger = logger;
        }

        public async Task<Veiculo> AdicionarAsync(string placa, string marca, string modelo, int ano, int clienteId)
        {
            try
            {
                var clienteExiste = await _clienteRepository.ObterPorIdAsync(clienteId.ToString())
                    ?? throw new KeyNotFoundException($"Cliente com Id '{clienteId}' não encontrado.");

                bool placaExiste = await _veiculoRepository.PlacaExisteAsync(placa);
                if (placaExiste)
                    throw new InvalidOperationException($"Já existe um veículo com a placa '{placa}'.");

                var veiculo = new Veiculo
                {
                    Placa = placa.ToUpper(),
                    Marca = marca,
                    Modelo = modelo,
                    Ano = ano,
                    ClienteId = clienteId,
                    Ativo = true,
                    DataCriacao = DateTime.Now
                };

                return await _veiculoRepository.AdicionarAsync(veiculo);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo VeiculoService:AdicionarAsync", _logger);
                throw;
            }
        }

        public async Task<Veiculo> AtualizarAsync(Veiculo veiculo)
        {
            try
            {
                var existente = await _veiculoRepository.ObterPorIdAsync(veiculo.Id.ToString())
                    ?? throw new KeyNotFoundException($"Veículo com Id '{veiculo.Id}' não encontrado.");

                var clienteExiste = await _clienteRepository.ObterPorIdAsync(veiculo.ClienteId.ToString())
                    ?? throw new KeyNotFoundException($"Cliente com Id '{veiculo.ClienteId}' não encontrado.");

                var placaEmUso = await _veiculoRepository.PlacaExisteAsync(veiculo.Placa);
                if (placaEmUso && existente.Placa != veiculo.Placa)
                    throw new InvalidOperationException($"Já existe um veículo com a placa '{veiculo.Placa}'.");

                veiculo.Placa = veiculo.Placa.ToUpper();
                veiculo.DataCriacao = existente.DataCriacao;
                veiculo.DataModificacao = DateTime.UtcNow;

                return await _veiculoRepository.AtualizarAsync(veiculo);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo VeiculoService:AtualizarAsync", _logger);
                throw;
            }
        }

        public async Task<IEnumerable<Veiculo>> ObterTodosAsync()
        {
            try
            {
                return await _veiculoRepository.ObterTodosAsync();
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo VeiculoService:ObterTodosAsync", _logger);
                throw;
            }
        }

        public async Task<IEnumerable<Veiculo>> ObterPorClienteIdAsync(string clienteId)
        {
            try
            {
                return await _veiculoRepository.ObterPorClienteIdAsync(int.Parse(clienteId));
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo VeiculoService:ObterPorClienteIdAsync", _logger);
                throw;
            }
        }

        public async Task<Veiculo?> ObterPorIdAsync(string id)
        {
            try
            {
                return await _veiculoRepository.ObterPorIdAsync(id);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo VeiculoService:ObterPorIdAsync", _logger);
                throw;
            }
        }

        public async Task RemoverAsync(string id)
        {
            try
            {
                var existente = await _veiculoRepository.ObterPorIdAsync(id)
                    ?? throw new KeyNotFoundException($"Veículo com Id '{id}' não encontrado.");

                await _veiculoRepository.RemoverAsync(id);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo VeiculoService:RemoverAsync", _logger);
                throw;
            }
        }
    }
}