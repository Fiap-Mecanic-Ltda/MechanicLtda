using AutoMapper;
using MechanicLtda.API.Controllers.Base;
using MechanicLtda.API.ViewModels;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace MechanicLtda.API.Controllers
{
    public class OrdemServicoController : BaseController
    {
        private readonly ILogger<OrdemServicoController> _logger;
        private readonly IOrdemServicoAppService _ordemServicoAppService;
        private readonly IMapper _mapper;

        public OrdemServicoController(INotificadorService notificadorService,
                                      ILogger<OrdemServicoController> logger,
                                      IOrdemServicoAppService ordemServicoAppService,
                                      IMapper mapper) : base(notificadorService)
        {
            _logger = logger;
            _ordemServicoAppService = ordemServicoAppService;
            _mapper = mapper;
        }

        /// <summary>Lista todas as Ordens de Serviço.</summary>
        [HttpGet]
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

        /// <summary>Retorna uma Ordem de Serviço pelo Id.</summary>
        [HttpGet("{id}")]
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

        /// <summary>Lista as Ordens de Serviço filtradas pelo ClienteId.</summary>
        [HttpGet("cliente/{clienteId}")]
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

        /// <summary>Lista as Ordens de Serviço filtradas pelo Status (usando Display).</summary>
        [HttpGet("status/{statusDescricao}")]
        public async Task<IActionResult> ObterPorStatus(string statusDescricao)
        {
            try
            {
                return CustomResponse(await _ordemServicoAppService.ObterPorStatusAsync(statusDescricao));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter ordens de serviço por status", _logger);
                return CustomResponse();
            }
        }

         /// <summary>Cria uma nova Ordem de Serviço com status inicial 'Recebida'. Retorna o ID da OS criada.</summary>
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] OrdemServicoCreateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return CustomResponse(ModelState);

                var dto = _mapper.Map<OrdemServicoCreateDto>(model);
                var response = await _ordemServicoAppService.AdicionarAsync(dto);

                if (!response.hasErrors)
                {
                    var responseViewModel = new OrdemServicoCreateResponseViewModel
                    {
                        Id = response.getResponse.Id,
                        Mensagem = "Ordem de Serviço criada com sucesso."
                    };
                    return CustomResponse(responseViewModel);
                }

                return CustomResponse(response);
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao criar ordem de serviço", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Atualiza uma Ordem de Serviço. Avança automaticamente para 'Em Diagnóstico' quando descrição e valor estiverem preenchidos.</summary>
        [HttpPut("{id}")]
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

        /// <summary>Move a OS para o status 'Em Diagnóstico'. Requer status atual: Recebida.</summary>
        [HttpPatch("{id}/em-diagnostico")]
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

        /// <summary>Move a OS para o status 'Aguardando Aprovação'. Requer status atual: Em Diagnóstico.</summary>
        [HttpPatch("{id}/aguardando-aprovacao")]
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

        /// <summary>Notificação externa: Cliente aprova a Ordem de Serviço. Requer status atual: Aguardando Aprovação.</summary>
        [HttpPatch("{id}/aprovar")]
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

        /// <summary>Notificação externa: Cliente recusa a Ordem de Serviço. Requer status atual: Aguardando Aprovação.</summary>
        [HttpPatch("{id}/recusar")]
        public async Task<IActionResult> Recusar(string id, [FromBody] RecusaOrdemServicoViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return CustomResponse(ModelState);

                return CustomResponse(await _ordemServicoAppService.RecusarAsync(id, model.MotivoRecusa));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao recusar ordem de serviço", _logger);
                return CustomResponse();
            }
        }

        /// <summary>Move a OS para o status 'Em Execução'. Requer status atual: Aguardando Aprovação.</summary>
        [HttpPatch("{id}/em-execucao")]
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

        /// <summary>Move a OS para o status 'Finalizada'. Requer status atual: Em Execução.</summary>
        [HttpPatch("{id}/finalizar")]
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

        /// <summary>Move a OS para o status 'Entregue'. Requer status atual: Finalizada.</summary>
        [HttpPatch("{id}/entregar")]
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

        /// <summary>Remove uma Ordem de Serviço.</summary>
        [HttpDelete("{id}")]
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