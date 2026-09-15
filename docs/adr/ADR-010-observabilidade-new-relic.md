# ADR-010 — Observabilidade com New Relic, e CloudWatch na borda serverless

- **Status:** aceito
- **Data:** 2026-09-13
- **Implementação:** MechanicLtda (agente e eventos), InfraKubernete (`observability/`, `k8s/observabilidade/`), Lambda (`infra/alarmes.tf`)

## Contexto

O desafio exige integração com Datadog ou New Relic e monitoramento de latência das APIs,
consumo de recursos do Kubernetes, healthchecks e uptime, alertas para falhas no processamento
de ordens de serviço e logs estruturados em JSON com correlação entre requisições. Exige também
dashboards de volume diário de OS, tempo médio de execução por status e erros nas integrações.

Os três painéis de negócio não saem de métricas de infraestrutura: dependem de eventos emitidos
pela aplicação.

## Decisão

**New Relic** como ferramenta principal:

| Necessidade | Solução |
|---|---|
| Latência e erros das APIs | Agente APM .NET instalado nas imagens da API e do Web, ligado pelo deployment quando a license key existe |
| CPU, memória, reinícios e HPA | Integração Kubernetes (`nri-bundle`) instalada pelo HelmChart do k3s, em `lowDataMode` |
| Healthcheck e uptime | Monitor sintético de ping em `/health` pela URL pública do gateway, de duas regiões |
| Volume de OS e tempo por status | Evento `OrdemServicoEvento` (criação e mudança de status, com minutos no status anterior) |
| Falha no processamento de OS | Evento `OrdemServicoFalha`, classificado em Negócio ou Sistema; só Sistema alerta |
| Falha nas integrações | Evento `FalhaIntegracao` (SMTP) e erros de transação no APM |
| Logs JSON com correlação | JSON console do .NET com escopos; o `CorrelationId` é o `requestId` do gateway; o agente encaminha os logs com o trace |
| Alertas, painel e monitor | Terraform com o provider `newrelic`, em stack próprio |

**CloudWatch na borda serverless.** O API Gateway e as Lambdas só aparecem no New Relic com a
integração AWS da conta. Para não deixar esse trecho sem alarme, ele tem logs JSON e alarmes
próprios: 5xx e latência no gateway, erros e throttling nas funções e pico de `4xx` em
`/auth/cpf`, publicados num tópico SNS.

**Por que New Relic e não Datadog:** o plano gratuito permanente do New Relic cobre APM, logs,
integração Kubernetes, painéis, alertas e monitores de ping; no Datadog, depois do período de
avaliação, APM e logs não fazem parte do plano gratuito.

**Por que eventos no domínio e não métricas de requisição:** o Web cria e movimenta ordens de
serviço chamando os serviços em processo, sem passar pela API ([ADR-005](ADR-005-comunicacao-sincrona-rest.md)).
Contar requisições HTTP à API perderia tudo o que é feito pelo Web.

## Alternativas

| Alternativa | Por que não |
|---|---|
| Datadog | Atende tecnicamente; o plano gratuito não cobre APM e logs após o trial |
| Só CloudWatch | Não atende à exigência de Datadog ou New Relic; painéis de negócio exigiriam métricas customizadas publicadas pela aplicação |
| OpenTelemetry com coletor próprio | Mais portável, porém exige operar o coletor nos nós `t3.small` e ainda assim um backend de destino |

## Consequências

- Tudo é opcional até a license key existir: sem ela a aplicação sobe com o agente desligado, e
  sem a API key do New Relic o workflow de observabilidade avisa e pula.
- Painel e alertas são versionados e revisados por PR, como o resto da infraestrutura.
- O agente APM e a integração Kubernetes consomem memória nos nós `t3.small`; o limit dos pods
  deve ser acompanhado depois de ligar ([ADR-006](ADR-006-escalabilidade-hpa-e-asg.md)).
- A monitoração fica dividida em dois lugares (New Relic e CloudWatch) até a integração AWS do
  New Relic ser ligada.
