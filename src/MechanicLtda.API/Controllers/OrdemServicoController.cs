using AutoMapper;
using MechanicLtda.Bootstrap.Authorization;
using MechanicLtda.API.Controllers.Base;
using MechanicLtda.API.ViewModels;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicLtda.API.Controllers
{
    public class OrdemServicoController : BaseController
    {
        private readonly ILogger<OrdemServicoController> _logger;
        private readonly IOrdemServicoAppService         _ordemServicoAppService;
        private readonly IMapper                         _mapper;

        public OrdemServicoController(INotificadorService      notificadorService,
                                      ILogger<OrdemServicoController> logger,
                                      IOrdemServicoAppService  ordemServicoAppService,
                                      IMapper                  mapper) : base(notificadorService)
        {
            _logger                 = logger;
            _ordemServicoAppService = ordemServicoAppService;
            _mapper                 = mapper;
        }

        // ─── Endpoints administrativos (Administrador + Funcionario) ─────────────

        /// <summary>Lista todas as Ordens de Serviço. [Admin]</summary>
        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> ObterTodos()
        {
            try
            {
                return CustomResponse(await _ordemServicoAppService.ObterTodosAsync());
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter ordens de serviço", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Retorna o tempo médio de execução das Ordens de Serviço finalizadas. [Admin]</summary>
        [HttpGet("tempo-medio-execucao")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> ObterTempoMedioExecucao()
        {
            try
            {
                return CustomResponse(await _ordemServicoAppService.ObterTempoMedioExecucaoAsync());
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter tempo médio de execução", _logger);
                return CustomResponse();
            }
        }
        /// <summary>Retorna uma Ordem de Serviço pelo Id. [Admin]</summary>
        [HttpGet("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> ObterPorId(string id)
        {
            try
            {
                return CustomResponse(await _ordemServicoAppService.ObterPorIdAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter ordem de serviço", _logger);
                return CustomResponse();
            }
        }

        /// <summary>
        /// Consulta o progresso das Ordens de Serviço de um cliente. [Admin + Cliente]
        /// O cliente só deve consultar o seu próprio clienteId.
        /// </summary>
        [HttpGet("cliente/{clienteId}")]
        [Authorize(Roles = Roles.AdminOuCliente)]
        public async Task<IActionResult> ObterPorCliente(string clienteId)
        {
            try
            {
                return CustomResponse(await _ordemServicoAppService.ObterPorClienteIdAsync(clienteId));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter ordens de serviço do cliente", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Cria uma nova Ordem de Serviço com status inicial 'Recebida'. [Admin]</summary>
        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Criar([FromBody] OrdemServicoCreateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return CustomResponse(ModelState);

                var dto = _mapper.Map<OrdemServicoCreateDto>(model);
                return CustomResponse(await _ordemServicoAppService.AdicionarAsync(dto));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao criar ordem de serviço", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Atualiza uma Ordem de Serviço. [Admin]</summary>
        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Atualizar(string id, [FromBody] OrdemServicoUpdateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return CustomResponse(ModelState);

                var dto = _mapper.Map<OrdemServicoUpdateDto>(model);
                return CustomResponse(await _ordemServicoAppService.AtualizarAsync(id, dto));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao atualizar ordem de serviço", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Move a OS para 'Em Diagnóstico'. [Admin]</summary>
        [HttpPatch("{id}/iniciar-diagnostico")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> IniciarDiagnostico(string id)
        {
            try
            {
                return CustomResponse(await _ordemServicoAppService.IniciarDiagnosticoAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao iniciar diagnóstico da ordem de serviço", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Move a OS para 'Aguardando Aprovação'. [Admin]</summary>
        [HttpPatch("{id}/aguardar-aprovacao")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> AguardarAprovacao(string id)
        {
            try
            {
                return CustomResponse(await _ordemServicoAppService.AguardarAprovacaoAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao mover ordem de serviço para Aguardando Aprovação", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Aprova a Ordem de Serviço, movendo para 'Em Execução'. Requer status atual: Aguardando Aprovação. [Admin]</summary>
        [HttpPatch("{id}/aprovar")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Aprovar(string id)
        {
            try
            {
                return CustomResponse(await _ordemServicoAppService.AprovarAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao aprovar ordem de serviço", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Recusa a Ordem de Serviço, voltando para 'Em Diagnóstico'. Requer status atual: Aguardando Aprovação. [Admin]</summary>
        [HttpPatch("{id}/recusar")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Recusar(string id, [FromBody] RecusaOrdemServicoViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return CustomResponse(ModelState);

                return CustomResponse(await _ordemServicoAppService.RecusarAsync(id, model.MotivoRecusa ?? string.Empty));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao recusar ordem de serviço", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Move a OS para 'Em Execução'. [Admin]</summary>
        [HttpPatch("{id}/iniciar-execucao")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> IniciarExecucao(string id)
        {
            try
            {
                return CustomResponse(await _ordemServicoAppService.IniciarExecucaoAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao iniciar execução da ordem de serviço", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Move a OS para 'Finalizada'. [Admin]</summary>
        [HttpPatch("{id}/finalizar")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Finalizar(string id)
        {
            try
            {
                return CustomResponse(await _ordemServicoAppService.FinalizarAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao finalizar ordem de serviço", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Move a OS para 'Entregue'. [Admin]</summary>
        [HttpPatch("{id}/entregar")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Entregar(string id)
        {
            try
            {
                return CustomResponse(await _ordemServicoAppService.EntregarAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao registrar entrega da ordem de serviço", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Remove uma Ordem de Serviço. [Admin]</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Remover(string id)
        {
            try
            {
                return CustomResponse(await _ordemServicoAppService.RemoverAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao remover ordem de serviço", _logger);
                return CustomResponse();
            }
        }
    }
}
