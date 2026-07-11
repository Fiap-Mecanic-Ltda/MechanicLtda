using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Domain.Interfaces.Services;
using MechanicLtda.Domain.Services.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MechanicLtda.Domain.Services
{
    public class ServicoOficinaService : BaseService, IServicoOficinaService
    {
        private readonly IServicoOficinaRepository _servicoRepository;
        private readonly ILogger<ServicoOficinaService> _logger;

        public ServicoOficinaService(
            ILogger<ServicoOficinaService> logger,
            IConfiguration configuration,
            INotificadorService notificadorService,
            IServicoOficinaRepository servicoRepository) : base(notificadorService, configuration)
        {
            _logger = logger;
            _servicoRepository = servicoRepository;
        }

        public async Task<ServicoOficina> AdicionarAsync(ServicoOficina servico)
        {
            try
            {
                servico.DataCadastro = DateTime.UtcNow;
                servico.DataAtualizacao = null;
                return await _servicoRepository.AdicionarAsync(servico);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo ServicoOficinaService:AdicionarAsync", _logger);
                throw;
            }
        }

        public async Task<ServicoOficina> AtualizarAsync(ServicoOficina servico)
        {
            try
            {
                var existente = await _servicoRepository.ObterPorIdAsync(servico.Id.ToString())
                    ?? throw new KeyNotFoundException($"Serviço com Id '{servico.Id}' não encontrado.");

                existente.Nome = servico.Nome;
                existente.Descricao = servico.Descricao;
                existente.ValorBase = servico.ValorBase;
                existente.Ativo = servico.Ativo;
                existente.DataAtualizacao = DateTime.UtcNow;

                return await _servicoRepository.AtualizarAsync(existente);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo ServicoOficinaService:AtualizarAsync", _logger);
                throw;
            }
        }

        public async Task RemoverAsync(string id)
        {
            try
            {
                _ = await _servicoRepository.ObterPorIdAsync(id)
                    ?? throw new KeyNotFoundException($"Serviço com Id '{id}' não encontrado.");

                await _servicoRepository.RemoverAsync(id);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo ServicoOficinaService:RemoverAsync", _logger);
                throw;
            }
        }

        public async Task<IEnumerable<ServicoOficina>> ObterTodosAsync()
        {
            try
            {
                return await _servicoRepository.ObterTodosAsync();
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo ServicoOficinaService:ObterTodosAsync", _logger);
                throw;
            }
        }

        public async Task<IEnumerable<ServicoOficina>> ObterAtivosAsync()
        {
            try
            {
                return await _servicoRepository.ObterAtivosAsync();
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo ServicoOficinaService:ObterAtivosAsync", _logger);
                throw;
            }
        }

        public async Task<ServicoOficina?> ObterPorIdAsync(string id)
        {
            try
            {
                return await _servicoRepository.ObterPorIdAsync(id);
            }
            catch (Exception ex)
            {
                Notificar(ex, "Ocorreu um erro no metodo ServicoOficinaService:ObterPorIdAsync", _logger);
                throw;
            }
        }
    }
}
