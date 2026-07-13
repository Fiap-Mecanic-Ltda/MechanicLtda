using MechanicLtda.API.Controllers.Base;
using MechanicLtda.API.ViewModels;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicLtda.API.Controllers
{
    [AllowAnonymous]
    public class AprovacaoOrdemServicoController : BaseController
    {
        private readonly ILogger<AprovacaoOrdemServicoController> _logger;
        private readonly IOrdemServicoAppService _ordemServicoAppService;

        public AprovacaoOrdemServicoController(INotificadorService notificadorService,
                                                ILogger<AprovacaoOrdemServicoController> logger,
                                                IOrdemServicoAppService ordemServicoAppService) : base(notificadorService)
        {
            _logger = logger;
            _ordemServicoAppService = ordemServicoAppService;
        }

        /// <summary>Acesso anônimo: cliente aprova a Ordem de Serviço através do link recebido por e-mail.</summary>
        [HttpGet("{token}/aprovar")]
        public async Task<IActionResult> Aprovar(string token)
        {
            return await ConfirmarAsync(token, aprovado: true);
        }

        /// <summary>Acesso anônimo: cliente recusa a Ordem de Serviço através do link recebido por e-mail.</summary>
        [HttpGet("{token}/recusar")]
        public async Task<IActionResult> Recusar(string token)
        {
            return await ConfirmarAsync(token, aprovado: false);
        }

        private async Task<IActionResult> ConfirmarAsync(string token, bool aprovado)
        {
            try
            {
                var response = await _ordemServicoAppService.ConfirmarAprovacaoPorTokenAsync(token, aprovado, null);

                if (!response.hasErrors)
                {
                    var responseViewModel = new AprovacaoOrdemServicoResponseViewModel
                    {
                        Id = response.getResponse.Id,
                        NovoStatus = response.getResponse.StatusDescricao,
                        Mensagem = aprovado ? "Ordem de Serviço aprovada com sucesso." : "Ordem de Serviço recusada com sucesso."
                    };
                    return CustomResponse(responseViewModel);
                }

                return CustomResponse(response);
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao confirmar aprovação/recusa da ordem de serviço via token", _logger);
                return CustomResponse();
            }
        }
    }
}
