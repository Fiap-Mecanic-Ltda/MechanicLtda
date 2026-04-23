using AutoMapper;
using MechanicLtda.API.Controllers.Base;
using MechanicLtda.API.ViewModels;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace MechanicLtda.API.Controllers
{
    public class VeiculoController : BaseController
    {
        private readonly ILogger<VeiculoController> _logger;
        private readonly IVeiculoAppService _veiculoAppService;
        private readonly IMapper _mapper;

        public VeiculoController(INotificadorService notificadorService,
                                 ILogger<VeiculoController> logger,
                                 IVeiculoAppService veiculoAppService,
                                 IMapper mapper) : base(notificadorService)
        {
            _logger = logger;
            _veiculoAppService = veiculoAppService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> ObterTodos()
        {
            try
            {
                return CustomResponse(await _veiculoAppService.ObterTodosAsync());
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter veículos", _logger);
                return CustomResponse();
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(string id)
        {
            try
            {
                return CustomResponse(await _veiculoAppService.ObterPorIdAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter veículo", _logger);
                return CustomResponse();
            }
        }

        [HttpGet("cliente/{clienteId}")]
        public async Task<IActionResult> ObterPorCliente(string clienteId)
        {
            try
            {
                return CustomResponse(await _veiculoAppService.ObterPorClienteIdAsync(clienteId));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao obter veículos do cliente", _logger);
                return CustomResponse();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] VeiculoCreateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return CustomResponse(ModelState);

                var dto = _mapper.Map<VeiculoCreateDto>(model);
                return CustomResponse(await _veiculoAppService.AdicionarAsync(dto));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao criar veículo", _logger);
                return CustomResponse();
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Atualizar(string id, [FromBody] VeiculoUpdateViewModel model)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            try
            {
                var dto = _mapper.Map<VeiculoUpdateDto>(model);
                return CustomResponse(await _veiculoAppService.AtualizarAsync(id, dto));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao atualizar veículo", _logger);
                return CustomResponse();
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remover(string id)
        {
            try
            {
                return CustomResponse(await _veiculoAppService.RemoverAsync(id));
            }
            catch (Exception ex)
            {
                GravaException(ex, "Falha ao remover veículo", _logger);
                return CustomResponse();
            }
        }
    }
}