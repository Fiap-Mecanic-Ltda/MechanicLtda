using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Web.Integracoes;
using MechanicLtda.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MechanicLtda.Web.Controllers
{
    /// <summary>
    /// Orçamento e pagamento da OS, no OrcamentoPagamento-service (pelo API Gateway no cluster).
    /// Com a Saga ligada, o pagamento aprovado é o que leva a OS para execução; aprovar sem
    /// pagar é só informativo, e recusar encerra o orçamento e cancela a OS.
    /// </summary>
    public class OrcamentosController : RazorControllerBase
    {
        private readonly IOrcamentoCliente _orcamentos;
        private readonly IPagamentoCliente _pagamentos;
        private readonly IOrdemServicoCliente _ordens;
        private readonly IClienteAppService _clientes;
        private readonly OrcamentoServicoOptions _opcoes;

        public OrcamentosController(
            IOrcamentoCliente orcamentos,
            IPagamentoCliente pagamentos,
            IOrdemServicoCliente ordens,
            IClienteAppService clientes,
            IOptions<IntegracoesOptions> opcoes)
        {
            _orcamentos = orcamentos;
            _pagamentos = pagamentos;
            _ordens = ordens;
            _clientes = clientes;
            _opcoes = opcoes.Value.OrcamentoService;
        }

        /// <summary>Orçamento da OS <paramref name="id"/>, com itens, totais e tentativas de pagamento.</summary>
        public async Task<IActionResult> Details(int id)
        {
            var ordem = await _ordens.ObterPorIdAsync(id.ToString());
            if (ordem.hasErrors)
            {
                FlashErrors(ordem);
                return RedirectToAction("Index", "OrdensServico");
            }

            var orcamento = await _orcamentos.ObterPorOrdemServicoAsync(id);
            FlashErrors(orcamento);

            IEnumerable<PagamentoDto> pagamentos = [];
            if (!orcamento.hasErrors && orcamento.getResponse is { } o)
            {
                var lista = await _pagamentos.ListarPorOrcamentoAsync(o.Id);
                FlashErrors(lista);
                if (!lista.hasErrors)
                    pagamentos = lista.getResponse.OrderByDescending(p => p.CriadoEm).ToList();
            }

            return View(new OrcamentoDetalheViewModel
            {
                Ordem = ordem.getResponse,
                Orcamento = orcamento.hasErrors ? null : orcamento.getResponse,
                Pagamentos = pagamentos
            });
        }

        /// <summary>
        /// PDF do orçamento. Passa pelo Web porque o serviço exige o JWT do usuário, que o
        /// navegador não tem (fica no cookie de sessão).
        /// </summary>
        public async Task<IActionResult> Pdf(int id, int ordemServicoId)
        {
            var pdf = await _orcamentos.ObterPdfAsync(id);
            if (pdf.hasErrors)
            {
                FlashErrors(pdf);
                return RedirectToAction(nameof(Details), new { id = ordemServicoId });
            }
            return File(pdf.getResponse, "application/pdf", $"orcamento-{id:D6}.pdf");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprovar(int id, int ordemServicoId)
        {
            var resposta = await _orcamentos.AprovarAsync(id);
            if (resposta.hasErrors)
                FlashErrors(resposta);
            else
                FlashSuccess("Orçamento aprovado. A OS segue para execução quando o pagamento for confirmado.");
            return RedirectToAction(nameof(Details), new { id = ordemServicoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Recusar(int id, int ordemServicoId, string? motivo)
        {
            var resposta = await _orcamentos.RecusarAsync(id, motivo);
            if (resposta.hasErrors)
                FlashErrors(resposta);
            else
                FlashSuccess("Orçamento recusado. A Saga cancela a OS e libera o que estava reservado.");
            return RedirectToAction(nameof(Details), new { id = ordemServicoId });
        }

        public async Task<IActionResult> Pagar(int id)
        {
            var orcamento = await _orcamentos.ObterPorIdAsync(id);
            if (orcamento.hasErrors)
            {
                FlashErrors(orcamento);
                return RedirectToAction("Index", "OrdensServico");
            }

            var o = orcamento.getResponse;
            if (!o.AceitaDecisao)
            {
                TempData["Error"] = $"O orçamento está {o.Status.Descricao().ToLowerInvariant()} e não aceita pagamento.";
                return RedirectToAction(nameof(Details), new { id = o.OrdemServicoId });
            }

            var model = new PagamentoFormViewModel
            {
                OrcamentoId = o.Id,
                OrdemServicoId = o.OrdemServicoId,
                RequisicaoId = Guid.NewGuid()
            };
            await PreencherPagadorAsync(model);
            Exibicao(model, o);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pagar(PagamentoFormViewModel model)
        {
            if (!ModelState.IsValid)
                return await ReexibirAsync(model);

            var identificacao = string.IsNullOrWhiteSpace(model.NumeroDocumento)
                ? null
                : new IdentificacaoDto(model.TipoDocumento ?? "CPF", new string(model.NumeroDocumento.Where(char.IsLetterOrDigit).ToArray()));

            var resposta = await _pagamentos.CriarAsync(new PagamentoCreateDto
            {
                RequisicaoId = model.RequisicaoId == Guid.Empty ? Guid.NewGuid() : model.RequisicaoId,
                OrcamentoId = model.OrcamentoId,
                Token = model.Token,
                PaymentMethodId = model.PaymentMethodId,
                IssuerId = model.IssuerId,
                Installments = model.Installments,
                Descricao = $"Orcamento {model.OrcamentoId:D6} - OS {model.OrdemServicoId}",
                Payer = new PagadorDto { Email = model.Email.Trim(), Identification = identificacao }
            });

            if (resposta.hasErrors)
            {
                AddErrors(resposta);
                // O token do cartão é de uso único: a nova tentativa precisa de outro.
                model.Token = string.Empty;
                ModelState.Remove(nameof(PagamentoFormViewModel.Token));
                return await ReexibirAsync(model);
            }

            var pagamento = resposta.getResponse;
            switch (pagamento.Status)
            {
                case StatusPagamento.Aprovado:
                    FlashSuccess("Pagamento aprovado. A Saga confirma o orçamento e leva a OS para execução.");
                    break;
                case StatusPagamento.Pendente:
                case StatusPagamento.EmProcessamento:
                case StatusPagamento.Autorizado:
                    FlashSuccess($"Pagamento {pagamento.Status.Descricao().ToLowerInvariant()} no Mercado Pago. Use \"Atualizar status\" para conferir.");
                    break;
                default:
                    TempData["Error"] = $"Pagamento {pagamento.Status.Descricao().ToLowerInvariant()}"
                        + (string.IsNullOrWhiteSpace(pagamento.StatusDetalhe) ? "." : $" ({pagamento.StatusDetalhe}).")
                        + " Tente outro cartão.";
                    break;
            }
            return RedirectToAction(nameof(Details), new { id = model.OrdemServicoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sincronizar(int id, int ordemServicoId)
        {
            var resposta = await _pagamentos.SincronizarAsync(id);
            if (resposta.hasErrors)
                FlashErrors(resposta);
            else if (resposta.getResponse is null)
                FlashSuccess("O Mercado Pago não trouxe mudança no pagamento.");
            else
                FlashSuccess($"Pagamento atualizado: {resposta.getResponse.Status.Descricao().ToLowerInvariant()}.");
            return RedirectToAction(nameof(Details), new { id = ordemServicoId });
        }

        private async Task<IActionResult> ReexibirAsync(PagamentoFormViewModel model)
        {
            var orcamento = await _orcamentos.ObterPorIdAsync(model.OrcamentoId);
            if (orcamento.hasErrors)
            {
                FlashErrors(orcamento);
                return RedirectToAction(nameof(Details), new { id = model.OrdemServicoId });
            }
            Exibicao(model, orcamento.getResponse);
            return View(nameof(Pagar), model);
        }

        private void Exibicao(PagamentoFormViewModel model, OrcamentoDto orcamento)
        {
            model.Orcamento = orcamento;
            model.MercadoPagoPublicKey = _opcoes.MercadoPagoPublicKey;
        }

        /// <summary>Sugere e-mail e documento do cliente da OS; o operador pode trocar.</summary>
        private async Task PreencherPagadorAsync(PagamentoFormViewModel model)
        {
            var ordem = await _ordens.ObterPorIdAsync(model.OrdemServicoId.ToString());
            if (ordem.hasErrors)
                return;
            var cliente = await _clientes.ObterPorIdAsync(ordem.getResponse.ClienteId.ToString());
            if (cliente.hasErrors || cliente.getResponse is null)
                return;

            model.Email = cliente.getResponse.Email ?? string.Empty;
            var documento = new string((cliente.getResponse.CpfCnpj ?? string.Empty).Where(char.IsDigit).ToArray());
            if (documento.Length is 11 or 14)
            {
                model.NumeroDocumento = documento;
                model.TipoDocumento = documento.Length == 11 ? "CPF" : "CNPJ";
            }
        }
    }
}
