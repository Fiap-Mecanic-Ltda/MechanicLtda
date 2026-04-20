using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MechanicLtda.Domain.Services.Base
{
    public abstract class BaseService
    {
        private readonly INotificadorService _notificadorService;
        protected readonly IConfiguration _configuration;

        protected BaseService(INotificadorService notificadorService, IConfiguration configuration)
        {
            _notificadorService = notificadorService;
            _configuration = configuration;
        }

        protected void Notificar(string mensagem)
        {
            _notificadorService.Handle(new Notificacao(mensagem));
        }

        protected void Notificar(Exception ex, string mensagem, ILogger _logger)
        {
            _logger.LogError(ex, mensagem);
            _notificadorService.Handle(new Notificacao(mensagem));
        }
    }
}
