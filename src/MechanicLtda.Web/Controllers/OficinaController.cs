using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Enums;
using MechanicLtda.Web.Integracoes;
using MechanicLtda.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MechanicLtda.Web.Controllers
{
    /// <summary>
    /// Tela do mecânico: fila de diagnóstico e de reparo do ExecucaoProducao-service. Só existe
    /// com Integracoes:ExecucaoService:Habilitado=true.
    /// </summary>
    public class OficinaController : RazorControllerBase
    {
        public const int LinhasDiagnostico = 6;

        private readonly IExecucaoCliente? _execucao;
        private readonly IOrdemServicoAppService _ordens;
        private readonly IEstoqueAppService _estoque;
        private readonly IServicoOficinaAppService _servicos;
        private readonly IntegracoesOptions _integracoes;

        public OficinaController(
            IOrdemServicoAppService ordens,
            IEstoqueAppService estoque,
            IServicoOficinaAppService servicos,
            IOptions<IntegracoesOptions> integracoes,
            IExecucaoCliente? execucao = null)
        {
            _ordens = ordens;
            _estoque = estoque;
            _servicos = servicos;
            _integracoes = integracoes.Value;
            _execucao = execucao;
        }

        public async Task<IActionResult> Index(string? status = null)
        {
            if (_execucao is null)
                return NotFound();

            var fila = await _execucao.FilaAsync(status);
            FlashErrors(fila);

            return View(new OficinaIndexViewModel
            {
                Fila = fila.hasErrors ? [] : fila.getResponse,
                Status = status,
                SagaNoOsService = _integracoes.OsService.Habilitado
            });
        }

        public async Task<IActionResult> Details(int id)
        {
            if (_execucao is null)
                return NotFound();

            var execucao = await _execucao.ObterAsync(id);
            if (execucao.hasErrors)
            {
                FlashErrors(execucao);
                return RedirectToAction(nameof(Index));
            }

            return View(await MontarDetalheAsync(execucao.getResponse));
        }

        /// <summary>
        /// Manual, para quando a Saga ainda não está ligada: põe a OS na fila de diagnóstico,
        /// como faria o comando IniciarDiagnostico.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Receber(int ordemServicoId)
        {
            if (_execucao is null)
                return NotFound();

            var ordem = await _ordens.ObterPorIdAsync(ordemServicoId.ToString());
            if (ordem.hasErrors)
            {
                FlashErrors(ordem);
                return RedirectToAction(nameof(Index));
            }

            var os = ordem.getResponse;
            var resposta = await _execucao.ReceberAsync(os.Id, os.ClienteId, os.VeiculoId, os.DescricaoProblema);
            return Resultado(resposta, $"OS #{os.Id} na fila de diagnóstico.", os.Id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Acao(int id, string acao, string? texto = null)
        {
            if (_execucao is null)
                return NotFound();

            var resposta = acao switch
            {
                "assumir" => await _execucao.IniciarDiagnosticoAsync(id),
                "falha" when !string.IsNullOrWhiteSpace(texto) => await _execucao.FalhaDiagnosticoAsync(id, texto),
                "falha" => new ResponseDto<ExecucaoDto>().addError("Informe o motivo da falha no diagnóstico."),
                "liberar" => await _execucao.LiberarExecucaoAsync(id),
                "iniciar" => await _execucao.IniciarAsync(id),
                "finalizar" => await _execucao.FinalizarAsync(id, texto),
                "cancelar" => await _execucao.CancelarAsync(id, texto),
                _ => new ResponseDto<ExecucaoDto>().addError("Ação inválida.")
            };

            return Resultado(resposta, "Execução atualizada.", id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Diagnostico(int id, DiagnosticoFormViewModel form)
        {
            if (_execucao is null)
                return NotFound();

            var itens = new List<ItemDiagnosticoDto>();
            var erros = new ResponseDto<ExecucaoDto>();
            var estoques = await _estoque.ObterTodosAsync();
            var tiposEstoque = estoques.hasErrors ? new Dictionary<int, TipoEstoque>() : estoques.getResponse.ToDictionary(e => e.Id, e => e.Tipo);

            foreach (var (linha, numero) in form.Itens.Select((l, i) => (l, i + 1)))
            {
                if (linha.Vazia)
                    continue;

                if (linha.Quantidade < 1)
                {
                    erros.addError($"Linha {numero}: informe a quantidade.");
                    continue;
                }

                var item = linha.Converter(tiposEstoque);
                if (item is null)
                    erros.addError($"Linha {numero}: item de estoque ou serviço não encontrado.");
                else
                    itens.Add(item);
            }

            if (!erros.hasErrors && itens.Count == 0)
                erros.addError("Registre ao menos um item no diagnóstico.");

            if (erros.hasErrors)
            {
                FlashErrors(erros);
                return RedirectToAction(nameof(Details), new { id });
            }

            var resposta = await _execucao.RegistrarDiagnosticoAsync(id, itens, form.Observacao);
            return Resultado(resposta, "Diagnóstico registrado e estoque reservado.", id);
        }

        private IActionResult Resultado(ResponseDto<ExecucaoDto> resposta, string sucesso, int id)
        {
            if (resposta.hasErrors)
                FlashErrors(resposta);
            else
                FlashSuccess(sucesso);

            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<OficinaDetalheViewModel> MontarDetalheAsync(ExecucaoDto execucao)
        {
            var model = new OficinaDetalheViewModel
            {
                Execucao = execucao,
                SagaNoOsService = _integracoes.OsService.Habilitado,
                Diagnostico = new DiagnosticoFormViewModel
                {
                    Itens = Enumerable.Range(0, LinhasDiagnostico).Select(_ => new LinhaDiagnosticoViewModel()).ToList()
                }
            };

            if (execucao.Status == "EmDiagnostico")
            {
                var estoques = await _estoque.ObterTodosAsync();
                var servicos = await _servicos.ObterAtivosAsync();
                FlashErrors(estoques);
                FlashErrors(servicos);
                model.Estoques = estoques.hasErrors ? [] : estoques.getResponse.OrderBy(e => e.Nome).ToList();
                model.Servicos = servicos.hasErrors ? [] : servicos.getResponse.OrderBy(s => s.Nome).ToList();
            }

            return model;
        }
    }
}
