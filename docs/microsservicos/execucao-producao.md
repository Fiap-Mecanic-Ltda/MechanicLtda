# Execução e Produção externalizada para microsserviço

- **Status:** microsserviço pronto, ainda sem publicação na AWS; **nada removido deste repositório ainda**
- **Data:** 2026-11-12
- **Repositórios do serviço:**
  [ExecucaoProducao-service](https://github.com/Fiap-Mecanic-Ltda/ExecucaoProducao-service) (API + participante da Saga),
  [ExecucaoProducao-InfraKubernete](https://github.com/Fiap-Mecanic-Ltda/ExecucaoProducao-InfraKubernete) (ECR, ALB, IAM e deploy no k3s) e
  [ExecucaoProducao-InfraSGBD](https://github.com/Fiap-Mecanic-Ltda/ExecucaoProducao-InfraSGBD) (tabelas DynamoDB)

Este documento registra o que saiu deste repositório para o microsserviço de Execução e Produção,
como ele está organizado, o que mudou de comportamento e a lista de remoções pendentes aqui.

> **Leia antes de apagar qualquer coisa.** Estoque e serviços da oficina deste repositório
> continuam sendo a fonte de verdade em produção. A remoção só é segura depois dos TODOs 1 a 3 da
> seção [TODO](#todo--ordem-segura-de-execução).

> **Onde está o código.** O serviço está na branch `desenvolvimento` do ExecucaoProducao-service e
> as infras na `homologacao` dos dois repositórios de infra. A `main` dos três ainda tem só o
> commit inicial. Os links abaixo apontam para essas branches.

## 1. O que foi externalizado

A Execução é um serviço novo, com banco próprio **não relacional** (Amazon DynamoDB, tabelas
`execucao-*`), imagem própria no ECR (`mechanicltda-execucao`) e pipeline própria. É um
**participante** da Saga orquestrada pelo OS Service ([ordem-servico.md](ordem-servico.md)): recebe
os comandos de diagnóstico e de execução e comunica a finalização. O que sai daqui está na
[ADR-004 do serviço](https://github.com/Fiap-Mecanic-Ltda/ExecucaoProducao-service/blob/desenvolvimento/docs/adr/ADR-004-divisao-execucao.md),
e a escolha do banco na
[ADR-003](https://github.com/Fiap-Mecanic-Ltda/ExecucaoProducao-service/blob/desenvolvimento/docs/adr/ADR-003-dynamodb.md).

| Capacidade | Onde estava aqui | Onde está agora |
|---|---|---|
| Cadastro e consulta de estoque (peças e insumos) | `EstoqueService` / `EstoqueController` | `EstoqueAppService` / `EstoqueController`, mesmas rotas `/api/estoque/*` |
| Reposição de estoque | `PATCH /api/estoque/{id}/reposicao` | idem |
| Reserva e baixa de estoque | baixa direta ao incluir item na OS | reserva no diagnóstico, baixa quando o pagamento libera a execução, devolução na compensação |
| Catálogo de serviços da oficina | `ServicoOficinaService` / `ServicoOficinaController` | mesmas rotas `/api/servicos-oficina/*` |
| Fila de execução | — (não existia) | `ExecucaoOrdemServico` e `GET /api/execucao/fila` |
| Diagnóstico (itens da OS) | itens lançados na OS, rota a rota | `POST /api/execucao/{id}/diagnostico`, que publica `DiagnosticoConcluido` |
| Tabelas `Estoques`, `ServicosOficina` | `MechanicLtdaDb` | `execucao-estoque`, `execucao-servicos` (DynamoDB) |

O que **não** saiu: `Cliente`, `Veiculo`, `Usuario`, login e a aplicação Web seguem aqui; a OS foi
para o OS Service e o orçamento para o Billing. A Execução não chama nenhum outro serviço em
tempo de execução: recebe comandos e responde com eventos. A única leitura deste repositório é
a carga inicial do estoque (`tools/ExecucaoService.Carga`), rodada uma vez na virada.

## 2. Arquitetura resultante

```text
                   ┌─────────── MechanicLtda (este repositorio) ───────────┐
Browser ─▶ Web     │  API: Cliente, Veiculo, Usuario, login                │
             │     └───────────────────────▲───────────────────────────────┘
             │ HttpClient                  │ carga inicial (uma vez, na virada):
             ▼  (PENDENTE, TODO 3)         │ GET /api/estoque e /api/servicos-oficina
   API Gateway (HTTP API) ─▶ VPC Link ─▶ ALB interno
     ├─ /api/execucao*, /api/estoque*, /api/servicos-oficina* ─┐  (prioridade 110)
     ├─ /api/ordemservico* ─▶ OS Service (prioridade 100)      │
     └─ /api/*  ─▶ aqui (acao padrao, :8080)                   │
                                                               ▼  NodePort 8082
                          ┌──────── ExecucaoProducao-service ────────┐
                          │  REST + consumidores da Saga             │
                          │  modo automatico (so demonstracao)       │
                          └───────┬───────────────────────▲──────────┘
                                  │                       │ comandos
                                  ▼                       │ IniciarDiagnostico, IniciarExecucao,
             DynamoDB: execucao-ordens, -estoque,         │ LiberarReservaEstoque
                       -servicos, -mensagens       RabbitMQ (namespace mensageria)
             (role dos nos do k3s, sem senha)             │ eventos ▲
                                                          ▼         │
                                                   OS Service (orquestrador)
```

Pontos centrais:

- **Banco por serviço, NoSQL.** Os acessos da Execução são sempre por chave ou por status, sem
  joins, por isso DynamoDB em modo sob demanda. É o banco não relacional da Fase 4 (o OS Service
  usa SQL Server).
- **Sem senha de banco.** Os pods usam as credenciais da role dos nós do k3s (IMDSv2). A policy
  `mechanicltda-prod-execucao-dynamodb` só alcança as tabelas `execucao-*` e seus índices, e é
  anexada às roles do server e dos workers.
- **Estoque atômico sem lock.** Reserva e baixa usam escrita condicional
  (`quantidadeAtual - quantidadeReservada >= :n`). Ids continuam `int`, gerados por contador
  atômico, e os itens migrados daqui mantêm os ids atuais.
- **Idempotência sem outbox transacional.** O MassTransit não tem outbox para DynamoDB. O serviço
  usa o outbox em memória (publica só se o consumidor terminar sem erro) e a tabela
  `execucao-mensagens` (TTL de 7 dias): uma entrega repetida republica a mesma resposta, e um
  reenvio legítimo (outra `dataHora`) é tratado.
- **Retry.** Erro técnico tenta 5 vezes com intervalo crescente e depois vai para a fila `_error`.
- **Escala.** Deployment com HPA de 1 a 3 réplicas, probes em `GET /health`.

### Status da OS dentro da Execução

São independentes dos status do OS Service, que só vê os eventos:

| Status | Quando |
|---|---|
| `AguardandoDiagnostico` | recebeu `IniciarDiagnostico` |
| `EmDiagnostico` | mecânico assumiu |
| `DiagnosticoConcluido` | itens registrados, estoque reservado, `DiagnosticoConcluido` publicado |
| `NaFila` | recebeu `IniciarExecucao` (pagamento confirmado), estoque baixado |
| `EmExecucao` | reparo iniciado, `ExecucaoIniciada` publicado |
| `Finalizada` | reparo concluído, `ExecucaoFinalizada` publicado |
| `Cancelada` | compensada, reservas devolvidas |

## 3. Estrutura dos repositórios

### ExecucaoProducao-service

```text
ExecucaoProducao-service/
├── src/
│   ├── ExecucaoService.API             # Controllers (execucao, estoque, servicos-oficina), Swagger, JWT, /health
│   ├── ExecucaoService.Bootstrap       # Composition root: DynamoDB, MassTransit, logs, roles
│   ├── ExecucaoService.Contratos       # Mensagens da Saga (cópia do OsService.Contratos)
│   ├── ExecucaoService.Application     # Casos de uso: execução, comandos da Saga, estoque, catálogo, carga
│   ├── ExecucaoService.Domain          # ExecucaoOrdemServico, Estoque, ReservaEstoque, ServicoOficina, status
│   └── ExecucaoService.Infrastructure  # Repositórios DynamoDB, consumidores, publicador, modo automático, New Relic
├── tests/
│   ├── ExecucaoService.UnitTests       # Domínio, aplicação, consumidores (test harness) e bootstrap
│   ├── ExecucaoService.API.Tests       # API em memória + BDD em Reqnroll (fila e compensações)
│   └── ExecucaoService.IntegrationTests # Repositórios contra o DynamoDB Local
├── tools/ExecucaoService.Carga         # Carga do estoque e do catálogo vindos daqui, mantendo os ids
├── docs/                               # Contratos, ADRs 001–004 e coleção do Postman
├── docker-compose.yml                  # DynamoDB Local + RabbitMQ + API (porta 8082)
└── .github/workflows/ci.yml            # build, testes, cobertura >= 80% (hoje 92%), Sonar, imagem; ECR na main
```

### ExecucaoProducao-InfraKubernete

```text
infra/   # ECR mechanicltda-execucao, role OIDC mechanicltda-prod-execucao-github-actions,
         # target group :8082, anexo da policy do DynamoDB nas roles dos nós,
         # regra do ALB (só com rotear_trafego_alb = true)
k8s/     # Namespace execucao, ConfigMap, Deployment, Service, HPA; overlay prod com NodePort 8082
.github/workflows/  # terraform.yml e deploy.yml (ConfigMap do DynamoDB e Secret a partir do SSM)
```

Lê o remote state do [InfraKubernete](https://github.com/Fiap-Mecanic-Ltda/InfraKubernete) e do
ExecucaoProducao-InfraSGBD. State próprio em `prod/execucao-k8s/terraform.tfstate`.

### ExecucaoProducao-InfraSGBD

```text
infra/   # tabelas execucao-ordens (GSI status-index), execucao-estoque (GSI tipo-index),
         # execucao-servicos e execucao-mensagens (TTL em expiraEm); PITR nas de negócio;
         # policy de acesso e os parâmetros /mechanicltda/prod/execucao/dynamodb-* no SSM
```

Não depende de rede (DynamoDB não usa VPC). State próprio em `prod/execucao-sgbd/terraform.tfstate`.

| Tabela | Chave | Conteúdo |
|---|---|---|
| `execucao-ordens` | `OS#<id>` / `EXECUCAO` e `OS#<id>` / `HIST#<dataHora>` | Estado da OS na Execução e histórico |
| `execucao-estoque` | `ESTOQUE#<id>` | Quantidade atual, reservada e mínima, mais o contador de ids |
| `execucao-servicos` | `SERVICO#<id>` | Catálogo de serviços |
| `execucao-mensagens` | `<mensagem>#<correlationId>#<osId>#<dataHora>` | Idempotência da Saga |

### Trava de publicação

Nos três repositórios, nada vai para a AWS enquanto a Repository Variable `PUBLICAR_AWS` não for
`true`. Mesmo com ela ligada, o tráfego continua aqui até `rotear_trafego_alb = true`, e a
mensageria continua desligada até `Mensageria__Habilitada = "true"` no ConfigMap.

## 4. O contrato com o OS Service

O contrato completo é o
[contratos-eventos.md do OS Service](https://github.com/Fiap-Mecanic-Ltda/OrdemServico-service/blob/desenvolvimento/docs/contratos-eventos.md)
(veja [ordem-servico.md](ordem-servico.md)). A parte da Execução:

| Comando (fila) | O que a Execução faz | Resposta |
|---|---|---|
| `IniciarDiagnostico` (`execucao-iniciar-diagnostico`) | Põe a OS na fila de diagnóstico; se já havia diagnóstico, reabre e devolve as reservas | `DiagnosticoConcluido` quando o mecânico registra os itens; `DiagnosticoFalhou` se ele registra a falha, se falta saldo ou se a OS já passou do diagnóstico |
| `IniciarExecucao` (`execucao-iniciar-execucao`) | Baixa o estoque reservado e põe a OS na fila de reparo | `ExecucaoIniciada` e `ExecucaoFinalizada`; ou `ExecucaoFalhou` (reservas devolvidas) se não houve diagnóstico ou faltou saldo |
| `LiberarReservaEstoque` (`execucao-liberar-reserva-estoque`) | Devolve as reservas e cancela a OS na Execução | nenhuma |

A regra atual do pagamento não muda nada aqui: o pagamento acontece com a OS em **Aguardando
Aprovação**, direto no Billing, e a Execução só recebe `IniciarExecucao` depois do
`PagamentoConfirmado`. Quando o orçamento é recusado ou a aprovação vence, a Execução recebe
`LiberarReservaEstoque`.

`DiagnosticoConcluido` leva os itens no formato `ItemOrdemServico` do contrato (`descricao`,
`tipo`, `quantidade`, `valorUnitario`, `estoqueId`, `servicoOficinaId`). É a Execução que
classifica cada item como `Peca`, `Insumo` ou `Servico`.

## 5. Diferenças de comportamento

Pontos em que o serviço **não** é idêntico ao código daqui:

1. **Reserva antes da baixa.** Aqui o estoque é baixado quando o item entra na OS. Na Execução, o
   diagnóstico só reserva; a baixa acontece quando o pagamento libera a execução, e a reserva
   volta ao estoque na compensação. `GET /api/estoque` passa a mostrar quantidade atual,
   reservada e disponível, e um item com reserva não pode ser removido.
2. **Itens da OS nascem no diagnóstico.** O mecânico registra peças, insumos e serviços na
   Execução, que os envia ao OS Service. A tela de itens da OS deste repositório deixa de ser o
   lugar de lançar peças com a Saga ligada.
3. **Fila de execução e histórico próprios**, com ordem de atendimento (`GET /api/execucao/fila`).
4. **Consistência eventual.** O status da OS no OS Service só muda quando o evento da Execução é
   processado.
5. **Erro de concorrência vira 409.** Duas gravações simultâneas no mesmo item respondem conflito
   em vez de sobrescrever.
6. **Modo automático** (`Execucao__ModoAutomatico=true`) avança as OS sozinho a cada
   `Execucao__SegundosPorPasso` segundos. Serve só para a demonstração e fica desligado no
   cluster.

## 6. Estado atual deste repositório

Nada foi alterado aqui. Em particular:

- As rotas `/api/estoque/*` e `/api/servicos-oficina/*` continuam respondendo.
- `MechanicLtda.Web` (`EstoquesController`, `ServicosOficinaController`,
  `ItensOrdemServicoController` e `HomeController`) continua usando `IEstoqueAppService` e
  `IServicoOficinaAppService` em processo.
- As tabelas `Estoques` e `ServicosOficina` continuam em `MechanicLtdaDb`, e `ItensOrdemServico`
  tem chave estrangeira para as duas.

## TODO — ordem segura de execução

A ordem importa: a carga do estoque precisa rodar antes de o tráfego virar, e nada pode ser
removido daqui enquanto o OS deste repositório ainda apontar para `Estoque` e `ServicoOficina`.

### TODO 1 — Publicar o serviço na AWS (sem mudar o tráfego)

- [ ] Promover `desenvolvimento` → `homologacao` → `main` no ExecucaoProducao-service e
      `homologacao` → `main` nos dois repositórios de infra.
- [ ] Promover a `homolog` do [InfraKubernete](https://github.com/Fiap-Mecanic-Ltda/InfraKubernete)
      para a `main` (PR #12 e PR #13: porta 8082 no SG dos nós e RabbitMQ no namespace
      `mensageria`) e cadastrar o secret `RABBITMQ_SEGREDO`.
- [ ] Ligar `PUBLICAR_AWS=true` e aplicar, nesta ordem: ExecucaoProducao-InfraSGBD (tabelas,
      policy e SSM), ExecucaoProducao-InfraKubernete (ECR, role, target group, policy nos nós) e o
      push na `main` do service (imagem e deploy). Copiar `github_actions_role_arn` para o secret
      `AWS_ROLE_ARN` do service e da infra, e criar o `EXECUCAO_INFRA_DISPATCH_TOKEN` no service.
- [ ] Conferir `GET /health` do pod e o New Relic.

### TODO 2 — Carregar o estoque e o catálogo

- [ ] Rodar `tools/ExecucaoService.Carga` contra a API daqui, com um JWT de Administrador. Ela
      grava estoque e serviços nas tabelas da Execução com os mesmos ids, sem reservas, e pode
      rodar de novo sem duplicar nada.
- [ ] A partir daqui, congelar alterações de estoque e serviços neste repositório até a virada
      (senão a carga fica desatualizada).

### TODO 3 — Virar o tráfego e ligar a mensageria

- [ ] `rotear_trafego_alb = true` no ExecucaoProducao-InfraKubernete (regra de prioridade 110).
- [ ] `MechanicLtda.Web`: trocar `IEstoqueAppService` e `IServicoOficinaAppService` por
      `HttpClient` tipado no gateway em `EstoquesController`, `ServicosOficinaController`,
      `ItensOrdemServicoController` (listas de peças e serviços) e `HomeController` (estoque
      crítico). Mostrar a quantidade reservada nas telas de estoque.
- [ ] Tela nova (ou reaproveitada) para o mecânico: fila de execução, diagnóstico, iniciar e
      finalizar o reparo.
- [ ] `Mensageria__Habilitada="true"` no ConfigMap da Execução, junto com a Saga do OS Service
      (`Saga__Habilitada="true"` e `Simuladores__Execucao=false`).
- [ ] Atualizar a cópia de `docs/contratos-eventos.md` do ExecucaoProducao-service, que ainda
      descreve o comando `SolicitarPagamento` e o status `AguardandoPagamento`, para ficar igual
      à do OS Service (regra do pagamento em Aguardando Aprovação). O código da Execução não
      muda.

### TODO 4 — Remover o que foi externalizado

Só depois de 1 a 3 estáveis **e** da remoção da OS ([ordem-servico.md](ordem-servico.md)),
porque `ItemOrdemServico` tem chave estrangeira para `Estoque` e `ServicoOficina`.

**Remover por completo:**

- [ ] `src/MechanicLtda.Domain/Entities/Estoque.cs`, `ServicoOficina.cs`
- [ ] `src/MechanicLtda.Domain/Enums/TipoEstoque.cs`
- [ ] `src/MechanicLtda.Domain/Interfaces/Repositories/IEstoqueRepository.cs`, `IServicoOficinaRepository.cs`
- [ ] `src/MechanicLtda.Domain/Interfaces/Services/IEstoqueService.cs`, `IServicoOficinaService.cs`
- [ ] `src/MechanicLtda.Domain/Services/EstoqueService.cs`, `ServicoOficinaService.cs`
- [ ] `src/MechanicLtda.Application/AppServices/EstoqueAppService.cs`, `ServicoOficinaAppService.cs` e as interfaces
- [ ] `src/MechanicLtda.Application/DTOs/EstoqueDtos.cs`, `ServicoOficinaDtos.cs`
- [ ] `src/MechanicLtda.Infrastructure/Repositories/EstoqueRepository.cs`, `ServicoOficinaRepository.cs`
- [ ] `src/MechanicLtda.Infrastructure/FluentAPI/ConfiguracaoTabelas/EstoqueConfig.cs`, `ServicoOficinaConfig.cs`
- [ ] `src/MechanicLtda.API/Controllers/EstoqueController.cs`, `ServicoOficinaController.cs`
- [ ] `src/MechanicLtda.API/ViewModels/EstoqueViewModels.cs`, `ServicoOficinaViewModels.cs`
- [ ] Testes: `EstoqueServiceTests`, `ServicoOficinaServiceTests`, `EstoqueAppServiceTests`,
      `ServicoOficinaAppServiceTests`, `EstoqueControllerTests`, `ServicoOficinaControllerTests`

**Editar (remover apenas o trecho):**

| Arquivo | O que sai |
|---|---|
| `src/MechanicLtda.Infrastructure/BancoAPIContext.cs` | linhas ~27–28: `DbSet` de `Estoques` e `ServicosOficina` |
| `src/MechanicLtda.Bootstrap/Extensions/DependencyInjectionExtension.cs` | linhas ~26, ~27, ~47, ~48, ~58 e ~59: repositórios, serviços e AppServices |
| `src/MechanicLtda.Bootstrap/Extensions/DataSeederExtension.cs` | linhas ~78–89: seed de `Estoques` |
| `src/MechanicLtda.Application/Mapping/SharedAutoMapperProfile.cs` | linhas ~23–26 e ~37 (`EstoqueDto` é montado à mão no AppService) |
| `src/MechanicLtda.API/Extensions/AutoMapperProfile.cs` | linhas ~25–29 |
| `src/MechanicLtda.Web/ViewModels/RazorViewModels.cs` | `EstoqueFormViewModel`, `EstoqueReposicaoRazorViewModel`, `ServicoOficinaFormViewModel` viram modelos do cliente HTTP |

**Banco e documentação:**

- [ ] Migration nova com `DROP TABLE` de `Estoques` e `ServicosOficina`, depois do drop de
      `ItensOrdemServico`. **Não** apagar `AddEstoque` nem `AddServicoOficinaTempoExecucao`.
- [ ] Antes do drop, conferir que as tabelas da Execução têm todos os itens (ou rodar a carga de
      novo).
- [ ] Atualizar o `README.md` (endpoints, modelo de domínio, repositórios do projeto) e os
      diagramas em [docs/arquitetura](../arquitetura/README.md).

## Referências

| Documento | Assunto |
|---|---|
| [README do serviço](https://github.com/Fiap-Mecanic-Ltda/ExecucaoProducao-service/blob/desenvolvimento/README.md) | Como rodar, rotas, Postman, carga, testes e esteira |
| [ADR-001 do serviço](https://github.com/Fiap-Mecanic-Ltda/ExecucaoProducao-service/blob/desenvolvimento/docs/adr/ADR-001-mensageria-rabbitmq.md) | RabbitMQ e MassTransit |
| [ADR-002 do serviço](https://github.com/Fiap-Mecanic-Ltda/ExecucaoProducao-service/blob/desenvolvimento/docs/adr/ADR-002-saga-orquestrada.md) | Saga orquestrada pelo OS Service |
| [ADR-003 do serviço](https://github.com/Fiap-Mecanic-Ltda/ExecucaoProducao-service/blob/desenvolvimento/docs/adr/ADR-003-dynamodb.md) | DynamoDB, tabelas e idempotência |
| [ADR-004 do serviço](https://github.com/Fiap-Mecanic-Ltda/ExecucaoProducao-service/blob/desenvolvimento/docs/adr/ADR-004-divisao-execucao.md) | O que sai deste repositório |
| [README da InfraKubernete](https://github.com/Fiap-Mecanic-Ltda/ExecucaoProducao-InfraKubernete/blob/homologacao/README.md) | ECR, ALB, acesso ao DynamoDB, deploy e primeira subida |
| [README da InfraSGBD](https://github.com/Fiap-Mecanic-Ltda/ExecucaoProducao-InfraSGBD/blob/homologacao/README.md) | Tabelas, policy e custo |
| [ordem-servico.md](ordem-servico.md) | O orquestrador da Saga |
