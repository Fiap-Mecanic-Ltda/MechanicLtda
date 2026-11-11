# Orçamento externalizado para microsserviço

- **Status:** microsserviço pronto; **nada removido deste repositório ainda**
- **Data:** 2026-11-06
- **Repositório do serviço:** [OrcamentoPagamento-service](https://github.com/Fiap-Mecanic-Ltda/OrcamentoPagamento-service)

Este documento registra o que saiu deste repositório para o microsserviço de orçamento, o
que mudou de comportamento, e a lista de remoções pendentes aqui.

> **Leia antes de apagar qualquer coisa.** O código de orçamento deste repositório continua
> sendo a fonte de verdade em produção. A remoção só é segura depois dos passos 1 e 2 da
> seção [TODO](#todo--ordem-segura-de-execução).

## 1. O que foi externalizado

O contexto de orçamento virou um serviço próprio, com banco próprio
(`MechanicOrcamentoDb`), imagem própria no ECR (`mechanicltda-orcamento`) e pipeline
própria. Isso revisa parcialmente a [ADR-005](../adr/ADR-005-comunicacao-sincrona-rest.md),
que havia descartado microsserviços na Fase 3 — o racional da mudança está na
[ADR-001 do serviço](https://github.com/Fiap-Mecanic-Ltda/OrcamentoPagamento-service/blob/main/docs/adr/ADR-001-extracao-do-orcamento-como-microsservico.md).

| Capacidade | Onde estava aqui | Onde está agora |
|---|---|---|
| Cálculo dos totais do orçamento | `OrcamentoService.CriarOuAtualizarAsync` | `OrcamentoService.CalcularAsync`, a partir do evento |
| Ajuste manual de valores | `OrcamentoService.AtualizarManualAsync` | idem, com `ValorTotalServicos` |
| Geração do PDF (QuestPDF) | `OrcamentoPdfAppService` | idem, alimentado por snapshot local |
| Rotas `/api/orcamento/*` | `OrcamentoController` | mesmo contrato, no serviço |
| Tabela `Orcamentos` | `MechanicLtdaDb` | `MechanicOrcamentoDb` |

O que **não** saiu: `OrdemServico`, `ItemOrdemServico`, `Estoque`, `ServicoOficina`,
`Cliente`, `Veiculo` e `Usuario` seguem aqui. O serviço de orçamento não lê nenhum deles.

## 2. Arquitetura resultante

```text
                   ┌─────────── MechanicLtda (este repositorio) ───────────┐
Browser ─▶ Web     │  API: Cliente, Veiculo, OrdemServico, Item, Estoque   │
             │     └───────────────────────┬───────────────────────────────┘
             │                             │ publica  ← PENDENTE (TODO 1)
             │ HttpClient                  ▼
             ▼                     SNS ordem-servico-eventos
   API Gateway (HTTP API)                   │
     ├─ /api/orcamento/* ─┐        SQS ordem-servico-itens-alterados ──▶ DLQ
     └─ /api/*  ─▶ aqui   │                 │
                          ▼                 ▼
                 ┌─── OrcamentoPagamento-service ───┐
                 │  REST + consumidor + PDF         │
                 │  Banco: MechanicOrcamentoDb      │
                 └──────────────┬───────────────────┘
                                ▼
                      SNS orcamento-eventos
```

## 3. O contrato que este repositório passa a cumprir

Este repositório vira **produtor** de um evento. O serviço de orçamento consome
`ordem-servico.itens-alterados`, que carrega o **estado atual completo** dos itens da OS
(não um delta), já classificados, mais os dados de OS/cliente/veículo usados no PDF.

A classificação é responsabilidade de quem publica — ou seja, daqui:

| Item da OS | `categoria` no evento |
|---|---|
| `EstoqueId` preenchido e `Estoque.Tipo == TipoEstoque.Peca` | `1` (Peca) |
| `EstoqueId` preenchido e `Estoque.Tipo == TipoEstoque.Insumo` | `2` (Insumo) |
| `ServicoOficinaId` preenchido, ou só `DescricaoServico` | `3` (Servico) |

Formato completo do payload, regras de idempotência e tratamento de erro no consumo:
[ADR-002 do serviço](https://github.com/Fiap-Mecanic-Ltda/OrcamentoPagamento-service/blob/main/docs/adr/ADR-002-contrato-de-eventos.md).

Existe também `POST /api/orcamento/recalcular` no serviço, com o mesmo payload e resposta
síncrona — é a saída para reprocessar mensagem que caiu na DLQ e para quando o chamador
precisar do orçamento na mesma requisição.

## 4. Diferenças de comportamento

Pontos em que o serviço **não** é idêntico ao código daqui:

1. **`ValorTotalServicos` é um campo novo.** Aqui o orçamento só separa peças e insumos, e
   itens de mão de obra entram apenas no total geral. No serviço há três categorias, e o
   total geral do ajuste manual é a soma das três. `OrcamentoUpdateDto` ganhou o campo, e a
   tela de ajuste manual vai precisar dele.
2. **Consistência eventual.** Hoje o orçamento é recalculado dentro da requisição que altera
   o item. Via fila, existe uma janela entre alterar o item e o orçamento refletir. A tela
   de detalhe da OS precisa tolerar ler um orçamento ainda não atualizado.
3. **O PDF usa snapshot.** Os dados de cliente/veículo do PDF são os do momento do cálculo.
   Se o cliente trocar de telefone e nenhum item mudar, o PDF mantém o telefone antigo até o
   próximo recálculo. Para um documento de orçamento isso é desejável.
4. **O total geral é a soma dos itens**, não a soma das categorias — para continuar fechando
   se uma categoria nova aparecer.

## 5. Estado atual deste repositório

Nada foi alterado aqui. Em particular:

- `ItemOrdemServicoService` continua chamando `OrcamentoService.CriarOuAtualizarAsync` em
  processo (linhas ~82, ~114 e ~141).
- `MechanicLtda.Web/Controllers/OrcamentosController` continua usando `IOrcamentoAppService`
  em processo.
- As rotas `/api/orcamento/*` deste repositório continuam respondendo.
- A tabela `Orcamentos` continua em `MechanicLtdaDb`.

## TODO — ordem segura de execução

A ordem importa: remover o código antes de ter o produtor publicando deixaria o sistema sem
orçamento nenhum.

### TODO 1 — Publicar o evento (sem remover nada)

- [ ] Provisionar SNS/SQS/DLQ e as permissões de IAM (ver [TODO 4](#todo-4--infraestrutura)).
- [ ] Publicar `ordem-servico.itens-alterados` em `src/MechanicLtda.Domain/Services/ItemOrdemServicoService.cs`,
      nos **três** pontos onde hoje há `_orcamentoService.CriarOuAtualizarAsync(...)`.
- [ ] Publicar **depois** do `SaveChanges` dos itens, para não emitir evento de transação que falhou.
- [ ] Propagar o `X-Correlation-Id` da requisição no campo `correlationId` do evento.
- [ ] `ocorridoEm` em UTC — é o campo que resolve evento fora de ordem no consumidor.
- [ ] Falha ao publicar deve ser *best-effort* (log + evento `FalhaIntegracao`), como já é o
      envio de e-mail. Não desfazer a alteração do item.
- [ ] **Manter o cálculo em processo nesta etapa.** Com os dois lados calculando, dá para
      comparar `MechanicLtdaDb.Orcamentos` com `MechanicOrcamentoDb.Orcamentos` e validar o
      serviço com tráfego real antes da virada.

### TODO 2 — Virar o consumo para o serviço

- [ ] Rota `/api/orcamento/{proxy+}` no API Gateway apontando para o Service do orçamento,
      declarada **antes** da rota curinga deste repositório, que é mais genérica.
- [ ] `MechanicLtda.Web/Controllers/OrcamentosController`: trocar `IOrcamentoAppService` por
      `HttpClient` tipado no gateway. O envelope de resposta é o mesmo `ResponseDto`, então
      as views, `FlashErrors` e `AddErrors` continuam valendo.
- [ ] `MechanicLtda.Web/Controllers/OrdensServicoController` (linha ~57): o `Details` também
      carrega o orçamento pelo AppService — precisa ir pelo HTTP, e tolerar falha sem
      derrubar a tela da OS.
- [ ] Incluir `ValorTotalServicos` em `OrcamentoFormViewModel` e na view de ajuste manual.
- [ ] `HttpClientFactory` com retry nos `GET` (idempotentes) e **sem** retry automático em
      `PUT`/`DELETE`.
- [ ] Rodar em paralelo o tempo que o time julgar necessário, comparando os dois bancos.

### TODO 3 — Remover o que foi externalizado

Só depois de 1 e 2 estáveis.

**Remover por completo:**

- [ ] `src/MechanicLtda.Domain/Entities/Orcamento.cs`
- [ ] `src/MechanicLtda.Domain/Interfaces/Services/IOrcamentoService.cs`
- [ ] `src/MechanicLtda.Domain/Interfaces/Repositories/IOrcamentoRepository.cs`
- [ ] `src/MechanicLtda.Domain/Services/OrcamentoService.cs`
- [ ] `src/MechanicLtda.Application/AppServices/OrcamentoAppService.cs`
- [ ] `src/MechanicLtda.Application/AppServices/OrcamentoPdfAppService.cs`
- [ ] `src/MechanicLtda.Application/AppServices/Interfaces/IOrcamentoAppService.cs`
- [ ] `src/MechanicLtda.Application/AppServices/Interfaces/IOrcamentoPdfService.cs`
- [ ] `src/MechanicLtda.Application/DTOs/OrcamentoDtos.cs`
- [ ] `src/MechanicLtda.Infrastructure/Repositories/OrcamentoRepository.cs`
- [ ] `src/MechanicLtda.Infrastructure/FluentAPI/ConfiguracaoTabelas/OrcamentoConfig.cs`
- [ ] `src/MechanicLtda.API/Controllers/OrcamentoController.cs`
- [ ] `src/MechanicLtda.API/ViewModels/OrcamentoViewModels.cs`
- [ ] `src/MechanicLtda.Web/Views/Orcamentos/` (a view de ajuste manual passa a ser a do fluxo HTTP)
- [ ] `tests/MechanicLtda.Domain.Tests/OrcamentoServiceTests.cs`
- [ ] `tests/MechanicLtda.Application.Tests/OrcamentoAppServiceTests.cs`
- [ ] `tests/MechanicLtda.Application.Tests/OrcamentoPdfAppServiceTests.cs`
- [ ] `tests/MechanicLtda.API.IntegrationTests/Controllers/OrcamentoControllerTests.cs`

**Editar (remover apenas o trecho):**

| Arquivo | O que sai |
|---|---|
| `src/MechanicLtda.Domain/Entities/OrdemServico.cs` | linha ~49: navegação `public Orcamento? Orcamento` |
| `src/MechanicLtda.Domain/Services/ItemOrdemServicoService.cs` | a dependência `IOrcamentoService` e as três chamadas `CriarOuAtualizarAsync` — fica só a publicação do evento |
| `src/MechanicLtda.Infrastructure/BancoAPIContext.cs` | linha ~29: `DbSet<Orcamento> Orcamentos` |
| `src/MechanicLtda.Bootstrap/Extensions/DependencyInjectionExtension.cs` | linhas ~28, ~49, ~60 e ~61: registros de repositório, serviço e AppServices de orçamento |
| `src/MechanicLtda.Bootstrap/Extensions/DataSeederExtension.cs` | linhas ~140–171: o seed de `Orcamentos` |
| `src/MechanicLtda.Application/Mapping/SharedAutoMapperProfile.cs` | linhas ~27 e ~36: mapeamentos de `Orcamento` |
| `src/MechanicLtda.API/Extensions/AutoMapperProfile.cs` | linha ~30: `OrcamentoUpdateViewModel → OrcamentoUpdateDto` |
| `src/MechanicLtda.Web/ViewModels/RazorViewModels.cs` | linha ~156 (`OrcamentoDto? Orcamento`) e `OrcamentoFormViewModel` (~179) — viram o DTO/ViewModel do cliente HTTP |
| `tests/MechanicLtda.Domain.Tests/ItemOrdemServicoServiceTests.cs` | as asserções sobre recálculo de orçamento, que passam a ser sobre publicação do evento |
| `src/MechanicLtda.Web/Views/OrdensServico/Details.cshtml` | só se o shape do dado de orçamento mudar na view |

**Banco e dependências:**

- [ ] Gerar uma migration nova que faz `DROP TABLE Orcamentos`. **Não** apagar
      `20260424022451_AddOrcamento` nem editar migrations antigas — o histórico tem de
      continuar aplicável num banco vazio.
- [ ] Antes do drop, confirmar que `MechanicOrcamentoDb` tem todos os orçamentos (ou migrar
      os que faltarem via `POST /api/orcamento/recalcular`).
- [ ] Remover o `PackageReference` do **QuestPDF** de `MechanicLtda.Application.csproj`.
- [ ] Remover as dependências nativas do QuestPDF/SkiaSharp do `src/MechanicLtda.API/Dockerfile`
      (`libfontconfig1`, `libfreetype6`, `fontconfig`, `fonts-dejavu-core` e o `fc-cache`) —
      **só se** nenhum outro PDF for gerado aqui. Conferir antes.
- [ ] Atualizar o `README.md`: tabela de endpoints, modelo de domínio, tecnologias e a seção
      de repositórios do projeto (passam a ser cinco).
- [ ] Marcar a [ADR-005](../adr/ADR-005-comunicacao-sincrona-rest.md) como parcialmente
      substituída, apontando para a ADR-001 do serviço.

### TODO 4 — Infraestrutura

No [InfraKubernete](https://github.com/Fiap-Mecanic-Ltda/InfraKubernete):

- [ ] Repositório ECR `mechanicltda-orcamento`.
- [ ] SNS/SQS:

  | Recurso | Nome | Observação |
  |---|---|---|
  | Tópico SNS | `ordem-servico-eventos` | publicado por este repositório |
  | Fila SQS | `ordem-servico-itens-alterados` | assinada no tópico, `RawMessageDelivery = true`, filter policy por `tipoEvento` |
  | Fila SQS | `ordem-servico-itens-alterados-dlq` | redrive com `maxReceiveCount = 5` |
  | Tópico SNS | `orcamento-eventos` | publicado pelo serviço |

  O script `scripts/localstack/01-criar-topico-e-fila.sh` do repositório do serviço é a
  referência exata dessa topologia.

- [ ] IAM: os pods deste repositório precisam de `sns:Publish` em `ordem-servico-eventos`;
      os do orçamento, de `sqs:ReceiveMessage`, `sqs:DeleteMessage`,
      `sqs:GetQueueAttributes` na fila e `sns:Publish` em `orcamento-eventos`.
- [ ] Manifestos do orçamento (Deployment, Service, HPA, Secret) com probes em `GET /health`.

No [InfraSGBD](https://github.com/Fiap-Mecanic-Ltda/InfraSGBD):

- [ ] Banco `MechanicOrcamentoDb` na instância RDS existente, com a connection string no SSM
      Parameter Store (`SecureString`). As migrations são aplicadas pelo próprio serviço na
      subida.

## Referências

| Documento | Assunto |
|---|---|
| [README do serviço](https://github.com/Fiap-Mecanic-Ltda/OrcamentoPagamento-service) | Como rodar, endpoints, configuração |
| [ADR-001 do serviço](https://github.com/Fiap-Mecanic-Ltda/OrcamentoPagamento-service/blob/main/docs/adr/ADR-001-extracao-do-orcamento-como-microsservico.md) | Por que extrair e por que banco por serviço |
| [ADR-002 do serviço](https://github.com/Fiap-Mecanic-Ltda/OrcamentoPagamento-service/blob/main/docs/adr/ADR-002-contrato-de-eventos.md) | Contrato do evento e idempotência |
| [ADR-003 do serviço](https://github.com/Fiap-Mecanic-Ltda/OrcamentoPagamento-service/blob/main/docs/adr/ADR-003-consumo-pelo-web-via-api-gateway.md) | Como o Web consome o serviço |
| [ADR-005 deste repo](../adr/ADR-005-comunicacao-sincrona-rest.md) | A decisão que esta extração revisa |
