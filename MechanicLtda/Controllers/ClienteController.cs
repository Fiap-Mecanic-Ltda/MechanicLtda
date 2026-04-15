using MechanicLtda.Application.AppServices.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MechanicLtda.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClienteController : ControllerBase
    {
        private readonly ILogger<ClienteController> _logger;
        private readonly IClienteAppService _clienteAppService;

        public ClienteController(ILogger<ClienteController> logger,
                                 IClienteAppService clienteAppService)
        {
            _logger = logger;
            _clienteAppService = clienteAppService;
        }
    }
}