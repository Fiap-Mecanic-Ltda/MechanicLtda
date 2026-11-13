# Ordem de Serviço externalizada para microsserviço

- **Status:** microsserviço pronto, ainda sem publicação na AWS; **nada removido deste repositório ainda**
- **Data:** 2026-11-12
- **Repositórios do serviço:**
  [OrdemServico-service](https://github.com/Fiap-Mecanic-Ltda/OrdemServico-service) (API + Saga),
  [OrdemServico-InfraKubernete](https://github.com/Fiap-Mecanic-Ltda/OrdemServico-InfraKubernete) (ECR, ALB e deploy no k3s) e
  [OrdemServico-InfraSGBD](https://github.com/Fiap-Mecanic-Ltda/OrdemServico-InfraSGBD) (RDS SQL Server próprio)

Este documento registra o que saiu deste repositório para o microsserviço de Ordem de Serviço
(OS Service), como ele está organizado, o que mudou de comportamento e a lista de remoções
pendentes aqui.

> **Leia antes de apagar qualquer coisa.** O código de OS deste repositório continua sendo a
> fonte de verdade em produção. A remoção só é segura depois dos TODOs 1 a 3 da seção
> [TODO](#todo--ordem-segura-de-execução).

> **Onde está o código.** O serviço está na branch `desenvolvimento` do OrdemServico-service e as
> infras na `homologacao` dos dois repositórios de infra. A `main` dos três ainda tem só o commit
> inicial. Os links abaixo apontam para essas branches.

## 1. O que foi externalizado

O ciclo de vida da OS virou um serviço próprio, com banco próprio (`OsServiceDb`, schema `os`,
numa instância RDS separada), imagem própria no ECR (`mechanicltda-os`) e pipeline própria. Além
de dono da OS, ele é o **orquestrador da Saga** da Fase 4, que coordena Orçamento e Pagamento
(Billing) e Execução e Produção. Isso substitui a
[ADR-005](../adr/ADR-005-comunicacao-sincrona-rest.md) ("comunicação síncrona, sem mensageria") —
o racional está na
[ADR-001 do serviço](https://github.com/Fiap-Mecanic-Ltda/OrdemServico-service/blob/desenvolvimento/docs/adr/ADR-001-mensageria-rabbitmq.md).

| Capacidade | Onde estava aqui | Onde está agora |
|---|---|---|
| Abertura, edição, consulta e exclusão da OS | `OrdemServicoService` / `OrdemServicoController` | `OrdemServicoAppService` / `OrdemServicoController`, mesmas rotas `/api/ordemservico/*` |
| Itens da OS | `ItemOrdemServicoService` / `ItemOrdemServicoController` | mesmas rotas `/api/ordem-servico/{id}/itens`, sem chave estrangeira para estoque e serviço |
| Transições de status | métodos do `OrdemServicoService` | entidade `OrdemServico.AlterarStatus` + tabela `TransicoesDeStatus` |
| Histórico de status | — (não existia) | `HistoricoStatusOrdemServico` e `GET /api/ordemservico/{id}/historico` |
| Tempo médio de execução | `GET /api/ordemservico/tempo-medio-execucao` | idem |
| Fluxo diagnóstico → orçamento → pagamento → execução | manual, rota a rota | Saga orquestrada (`OrdemServicoStateMachine`, MassTransit 8) |
| Tabelas `OrdensServico`, `ItensOrdemServico` | `MechanicLtdaDb` | `OsServiceDb` (schema `os`) |

O que **não** saiu: `Cliente`, `Veiculo`, `Usuario`, login e a aplicação Web seguem aqui. O OS
Service guarda cliente e veículo só por Id e, na abertura da OS, chama a API deste repositório
(`GET /api/cliente/{id}` e `GET /api/veiculo/{id}`) para validar que o veículo é do cliente e
obter nome e e-mail. `Estoque` e `ServicoOficina` vão para a Execução
([execucao-producao.md](execucao-producao.md)); `Orcamento` vai para o Billing
([orcamento.md](orcamento.md)).

## 2. Arquitetura resultante

```text
                   ┌─────────── MechanicLtda (este repositorio) ───────────┐
Browser ─▶ Web     │  API: Cliente, Veiculo, Usuario, login                │
             │     └──────────────▲────────────────────────────────────────┘
             │ HttpClient         │ REST: valida cliente/veiculo na abertura
             ▼  (PENDENTE, TODO 2)│ (JWT e X-Correlation-Id repassados)
   API Gateway (HTTP API) ─▶ VPC Link ─▶ ALB interno
     ├─ /api/ordemservico*, /api/OrdemServico*, /api/ordem-servico/* ─┐  (prioridade 100)
     └─ /api/*  ─▶ aqui (acao padrao, :8080)                          │
                                                                      ▼  NodePort 8081
                                         ┌──────── OrdemServico-service ────────┐
                                         │  REST + Saga (orquestrador)          │
                                         │  Banco: OsServiceDb (RDS, schema os) │
                                         │  outbox/inbox do MassTransit         │
                                         └──────────────┬───────────────────────┘
                                                        │ comandos  ▲ eventos
                                                        ▼           │
                                      RabbitMQ (namespace mensageria do k3s)
                                         │                          │
                               ┌─────────┴──────┐          ┌────────┴────────────┐
                               │ Billing        │          │ Execucao e Producao │
                               │ (orcamento e   │          │ (diagnostico, fila, │
                               │  pagamento)    │          │  estoque)           │
                               └────────────────┘          └─────────────────────┘
```

Pontos centrais:

- **Banco por serviço.** O RDS da OS só aceita a porta 1433 vinda dos nós do k3s, e nenhum outro
  serviço lê as tabelas dele. A connection string fica no SSM
  (`/mechanicltda/prod/os/db-connection-string`, `SecureString`) e vira Secret no deploy.
- **Saga orquestrada.** O OS Service publica comandos (`IniciarDiagnostico`, `GerarOrcamento`,
  `IniciarExecucao`, `CancelarOrcamento`, `EstornarPagamento`, `LiberarReservaEstoque`) e reage aos
  eventos de Billing e Execução. As compensações ficam declaradas num lugar só, a state machine
  ([ADR-002 do serviço](https://github.com/Fiap-Mecanic-Ltda/OrdemServico-service/blob/desenvolvimento/docs/adr/ADR-002-saga-orquestrada.md)).
- **Consistência.** Status da OS, estado da Saga e mensagens publicadas são gravados na mesma
  transação (outbox/inbox do MassTransit no SQL Server). O estado da Saga fica em
  `os.SagaOrdemServico` com lock pessimista: duas mensagens da mesma OS nunca são processadas ao
  mesmo tempo.
- **Prazos sem agendador.** Prazo de diagnóstico (48 h) e de aprovação (72 h ou a `validade` do
  orçamento) ficam em colunas do estado da Saga e são verificados a cada minuto pelo
  `VerificadorPrazosSaga`.
- **Filas.** Cada evento externo chega numa fila própria `os-<evento>` (JSON puro, sem cabeçalhos
  do MassTransit) e é repassado à fila da Saga `os-ordem-servico-saga`.
- **Escala.** Deployment com HPA de 1 a 3 réplicas, probes em `GET /health`, pod sem root.

## 3. Estrutura dos repositórios

### OrdemServico-service

Mesmas camadas deste repositório, mais o projeto de contratos:

```text
OrdemServico-service/
├── src/
│   ├── OsService.API             # Controllers, Swagger, JWT, CorrelationId, tratamento de erros, /health
│   ├── OsService.Bootstrap       # Composition root: banco, migrations, MassTransit, logs, roles
│   ├── OsService.Contratos       # Comandos e eventos da Saga (namespace MechanicLtda.Contratos)
│   ├── OsService.Application     # AppService, Saga (state machine, filas, estado), interfaces
│   ├── OsService.Domain          # OrdemServico, ItemOrdemServico, Historico, status e transições
│   └── OsService.Infrastructure  # EF Core (schema os), migrations, gateway do monolito,
│                                 # publicador, verificador de prazos, simuladores, New Relic
├── tests/
│   ├── OsService.UnitTests       # Domínio, aplicação, infraestrutura e a Saga com o test harness
│   └── OsService.API.Tests       # API em memória + BDD em Reqnroll (fluxo completo e compensações)
├── docs/                         # contratos-eventos.md e ADRs
├── docker-compose.yml            # SQL Server + RabbitMQ + API (porta 8081)
└── .github/workflows/ci.yml      # build, testes, cobertura >= 80%, Sonar, imagem; ECR na main
```

### OrdemServico-InfraKubernete

```text
infra/   # ECR mechanicltda-os, role OIDC mechanicltda-prod-os-github-actions,
         # target group :8081 e regra do ALB (só com rotear_trafego_alb = true)
k8s/     # Namespace ordem-servico, ConfigMap, Deployment, Service, HPA; overlay prod com NodePort 8081
.github/workflows/  # terraform.yml (plan/apply) e deploy.yml (monta o Secret a partir do SSM)
```

Lê do remote state do [InfraKubernete](https://github.com/Fiap-Mecanic-Ltda/InfraKubernete)
(VPC, nós do k3s, listener do ALB, provider OIDC). State próprio em `prod/os-k8s/terraform.tfstate`.

### OrdemServico-InfraSGBD

```text
infra/   # db subnet group, SG do RDS (1433 só dos nós), instância mechanicltda-prod-os
         # (db.t3.micro SQL Server Express) e a connection string no SSM
```

Não cria rede nem schema: a VPC vem do InfraKubernete e as migrations são aplicadas pela API na
subida. State próprio em `prod/os-sgbd/terraform.tfstate`.

### Trava de publicação

Nos três repositórios, nada vai para a AWS enquanto a Repository Variable `PUBLICAR_AWS` não for
`true`. Mesmo com ela ligada, o tráfego continua aqui até `rotear_trafego_alb = true`, e a Saga
continua desligada até `Saga__Habilitada = "true"` no ConfigMap.

## 4. O contrato entre os serviços

O contrato completo (comandos, eventos, campos, filas, idempotência, prazos e status) está em
[contratos-eventos.md](https://github.com/Fiap-Mecanic-Ltda/OrdemServico-service/blob/desenvolvimento/docs/contratos-eventos.md).
Uma cópia idêntica deve existir nos repositórios dos três serviços. Resumo:

| Regra | Valor |
|---|---|
| Broker | RabbitMQ no k3s (namespace `mensageria`) |
| Exchange | `fanout` com o nome da mensagem, ex.: `MechanicLtda.Contratos:GerarOrcamento` |
| Fila do consumidor | `<servico>-<mensagem-em-kebab>`, ex.: `billing-gerar-orcamento` |
| Corpo | JSON puro, `camelCase`, datas ISO 8601 UTC |
| Correlação | `correlationId` (GUID gerado na abertura da OS) + cabeçalho `x-correlation-id` para o New Relic |
| Entrega | at-least-once: consumidores idempotentes |

**Regra do pagamento (PR #9 do OrdemServico-service).** O pagamento é feito com a OS em
**Aguardando Aprovação**: não existe comando de cobrança, o cliente aprova o orçamento pagando,
direto no Billing. `PagamentoConfirmado` vale como aprovação e leva a OS para Em Execução.
`PagamentoFalhou` não cancela nada: a OS continua em Aguardando Aprovação para nova tentativa até
o prazo. `PagamentoConfirmado` fora de Aguardando Aprovação é ignorado (antes do orçamento) ou
estornado (durante uma compensação).

Fluxo feliz da Saga:

```text
iniciar-diagnostico ─▶ IniciarDiagnostico ─▶ Execucao
                       DiagnosticoConcluido ◀─┘   (OS: AguardandoOrcamento)
                       GerarOrcamento ─▶ Billing
                       OrcamentoGerado ◀─┘        (OS: AguardandoAprovacao)
          cliente paga no Billing ─▶ PagamentoConfirmado (OS: EmExecucao)
                       IniciarExecucao ─▶ Execucao
                       ExecucaoIniciada / ExecucaoFinalizada ◀─┘ (OS: Finalizada)
```

Compensações:

| Falha | Compensação | Status final |
|---|---|---|
| `DiagnosticoFalhou` | nenhuma | Recebida |
| `OrcamentoFalhou` | reenvia `IniciarDiagnostico` | Em Diagnóstico |
| `OrcamentoRecusado` | `LiberarReservaEstoque` | Cancelada |
| `PagamentoFalhou` | nenhuma (nova tentativa até o prazo) | Aguardando Aprovação |
| `PagamentoConfirmado` com a OS em compensação | `EstornarPagamento` | Cancelada |
| `ExecucaoFalhou` | `EstornarPagamento` + `CancelarOrcamento` | Cancelada |
| Prazo de aprovação vencido | `CancelarOrcamento` + `LiberarReservaEstoque` | Cancelada |
| Prazo de diagnóstico vencido | `LiberarReservaEstoque` | Recebida |
| Cancelamento manual durante o diagnóstico | `LiberarReservaEstoque` | Cancelada |

O OS Service também publica `OrdemServicoStatusAlterado` a cada mudança de status, para
notificações e painéis.

## 5. Diferenças de comportamento

Pontos em que o serviço **não** é idêntico ao código daqui:

1. **Status novos.** Os valores 1 a 6 são os mesmos daqui. Entram `AguardandoOrcamento = 7`,
   `Compensando = 9` e `Cancelada = 10`. O 8 é reservado e não deve ser usado (era
   `AguardandoPagamento`, removido com a regra do pagamento). As telas do Web, o filtro da
   listagem e o dashboard precisam conhecer os três novos.
2. **Transições validadas no domínio.** Toda mudança passa por `TransicoesDeStatus` e grava uma
   linha no histórico com a origem (`Usuario` ou `Saga`).
3. **Com a Saga ligada, a API não faz as transições da Saga.** `aguardar-aprovacao`, `aprovar`
   (ou `iniciar-execucao`), `recusar` e `finalizar` respondem 409, e `cancelar` só vale antes do
   orçamento. Com `Saga__Habilitada=false` (o padrão no cluster) a API funciona como aqui.
4. **Itens só em Recebida ou Diagnóstico**, e sem chave estrangeira: `estoqueId` e
   `servicoOficinaId` são ids da Execução, e cada item carrega `tipo` (`Peca`, `Insumo`,
   `Servico`) e `valorUnitario`.
5. **Sem e-mail.** Daqui saem o e-mail de mudança de status e o e-mail de aprovação com link
   (`OrdemServicoAprovacaoToken` e `AprovacaoOrdemServicoController`). O OS Service não envia
   e-mail: a aprovação passa a ser o pagamento no Billing (que recebe `nomeCliente` e
   `emailCliente` no `GerarOrcamento`), e as notificações de status devem assinar
   `OrdemServicoStatusAlterado`. Quem vai enviar essas notificações ainda não foi definido.
6. **Consistência eventual com cliente e veículo.** Se a API daqui não responder na abertura, o
   OS Service devolve 503 (com timeout e retry no `HttpClient`).

## 6. Estado atual deste repositório

OS e itens saíram daqui: o Web só fala com o OS Service (`Integracoes:OsService:BaseUrl`,
obrigatória). `OrdensServicoController`, `ItensOrdemServicoController` e `HomeController` usam
`OrdemServicoHttpAppService` e `ItensOrdemServicoHttp` (`src/MechanicLtda.Web/Integracoes`), que
repassam o JWT do login e o `X-Correlation-Id`; os status 7, 9 e 10 aparecem nas telas; a OS
mostra o histórico, os ids de orçamento e pagamento do Billing e o botão Cancelar. O
docker-compose de [integracao/](../../integracao/README.md) sobe tudo junto para teste.

A API deste repositório não tem mais `/api/ordemservico/*`, `/api/ordem-servico/{id}/itens`,
`/api/orcamento/*` nem a aprovação por token. As tabelas antigas (`OrdensServico`,
`ItensOrdemServico`, `OrdemServicoAprovacaoTokens`, `Orcamentos`) continuam em `MechanicLtdaDb`
como histórico.

## TODO — ordem segura de execução

A ordem importa: remover o código daqui antes de o Web falar com o serviço deixaria o sistema sem
OS nenhuma.

### TODO 1 — Publicar o serviço na AWS (sem mudar o tráfego)

- [ ] Promover `desenvolvimento` → `homologacao` → `main` no OrdemServico-service e
      `homologacao` → `main` nos dois repositórios de infra.
- [ ] Promover a `homolog` do [InfraKubernete](https://github.com/Fiap-Mecanic-Ltda/InfraKubernete)
      para a `main` (PR #12: porta 8081, outputs para os microsserviços e o RabbitMQ no namespace
      `mensageria`) e cadastrar o secret `RABBITMQ_SEGREDO`.
- [ ] Ligar `PUBLICAR_AWS=true` e aplicar, nesta ordem: OrdemServico-InfraSGBD (banco e SSM),
      OrdemServico-InfraKubernete (ECR, role, target group) e o push na `main` do service (imagem
      e deploy). Copiar `github_actions_role_arn` para o secret `AWS_ROLE_ARN` do service e da
      infra.
- [ ] Conferir `GET /health` do pod e o New Relic.

### TODO 2 — Virar o tráfego para o serviço

- [ ] `rotear_trafego_alb = true` no OrdemServico-InfraKubernete (regra de prioridade 100).
- [x] `MechanicLtda.Web`: trocar `IOrdemServicoAppService` e `IItemOrdemServicoAppService` por
      `HttpClient` tipado em `OrdensServicoController`, `ItensOrdemServicoController` e
      `HomeController`.
- [ ] `Integracoes__OsService__BaseUrl` no ConfigMap do Web (InfraKubernete), apontando para o API
      Gateway ou para o Service `os-service.ordem-servico.svc.cluster.local`.
- [x] Incluir os status 7, 9 e 10 nas views e no dashboard.
- [ ] Decidir o que fazer com as OS que já existem em `MechanicLtdaDb`: migrar para `OsServiceDb`
      (não há ferramenta de carga pronta) ou deixar as antigas como histórico somente leitura.
- [ ] `HttpClientFactory` com retry só nos `GET` (hoje há timeout, sem retry).

### TODO 3 — Ligar a Saga

- [ ] Billing consumindo `GerarOrcamento`, `CancelarOrcamento` e `EstornarPagamento` no RabbitMQ
      e publicando os eventos do contrato. Hoje o OrcamentoPagamento-service usa SNS/SQS e não
      implementa esse contrato (ver [orcamento.md](orcamento.md)); até lá o Billing fica
      simulado (`Simuladores__Billing`), o que **não** vale para produção.
- [ ] Execução com `Mensageria__Habilitada="true"` (ver [execucao-producao.md](execucao-producao.md)).
- [ ] `Saga__Habilitada="true"` no ConfigMap do OS Service e novo deploy.
- [ ] Definir quem envia as notificações de status (assinando `OrdemServicoStatusAlterado`) e
      desligar a aprovação por token daqui.

### TODO 4 — Remover o que foi externalizado (feito)

- [x] Domínio, aplicação, API, repositórios, configurações do EF, seed e testes de OS, itens,
      aprovação por token e orçamento saíram deste repositório, junto com o envio de e-mail
      (`IEmailService`, MailKit) e os eventos de negócio para o New Relic, que só a OS usava.
- [x] O Web chama só o OS Service: contratos em `src/MechanicLtda.Web/Integracoes`, sem as
      chaves `Habilitado`. Sem `Integracoes:OsService:BaseUrl` o Web não sobe.
- [x] Migration `SaidaOsEstoqueParaMicrosservicos`: as tabelas `OrdensServico`,
      `ItensOrdemServico`, `OrdemServicoAprovacaoTokens` e `Orcamentos` **ficam** no banco como
      histórico, sem perda de dados. Só caem as chaves estrangeiras de `OrdensServico` para
      `Clientes` e `Veiculos`, que travariam a exclusão de cliente ou veículo. Apagar as tabelas
      fica para quando a decisão do TODO 2 sobre as OS antigas for tomada.
- [x] `README.md` atualizado (endpoints, modelo de domínio, seed).
- [ ] Notificação de status ao cliente: hoje ninguém envia. Quem assumir assina
      `OrdemServicoStatusAlterado`.
- [ ] Diagramas em [docs/arquitetura](../arquitetura/README.md) e a
      [ADR-005](../adr/ADR-005-comunicacao-sincrona-rest.md) marcada como substituída pela
      ADR-001 do OS Service.

## Referências

| Documento | Assunto |
|---|---|
| [README do serviço](https://github.com/Fiap-Mecanic-Ltda/OrdemServico-service/blob/desenvolvimento/README.md) | Como rodar, endpoints, configuração, testes e esteira |
| [Contratos de mensagens](https://github.com/Fiap-Mecanic-Ltda/OrdemServico-service/blob/desenvolvimento/docs/contratos-eventos.md) | Comandos, eventos, regra do pagamento e status |
| [ADR-001 do serviço](https://github.com/Fiap-Mecanic-Ltda/OrdemServico-service/blob/desenvolvimento/docs/adr/ADR-001-mensageria-rabbitmq.md) | RabbitMQ e MassTransit |
| [ADR-002 do serviço](https://github.com/Fiap-Mecanic-Ltda/OrdemServico-service/blob/desenvolvimento/docs/adr/ADR-002-saga-orquestrada.md) | Saga orquestrada e compensações |
| [README da InfraKubernete](https://github.com/Fiap-Mecanic-Ltda/OrdemServico-InfraKubernete/blob/homologacao/README.md) | ECR, ALB, deploy e primeira subida |
| [README da InfraSGBD](https://github.com/Fiap-Mecanic-Ltda/OrdemServico-InfraSGBD/blob/homologacao/README.md) | RDS da OS e custo |
| [ADR-005 deste repo](../adr/ADR-005-comunicacao-sincrona-rest.md) | A decisão que esta extração substitui |
