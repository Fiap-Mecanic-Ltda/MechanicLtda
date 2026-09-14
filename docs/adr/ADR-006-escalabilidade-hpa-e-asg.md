# ADR-006 — Escalabilidade em dois níveis: HPA nos pods e Auto Scaling Group nos nós

- **Status:** aceito
- **Data:** 2026-09-13
- **Implementação:** repositório InfraKubernete — `k8s/base/api-hpa.yaml`, `k8s/base/web-hpa.yaml`, `infra/asg.tf`

## Contexto

A expansão para várias unidades aumenta a carga de forma irregular: picos no início do
expediente e na abertura de ordens de serviço. O desafio exige cluster Kubernetes com
escalabilidade. Os nós são `t3.small` (2 vCPU, 2 GiB), então só aumentar réplicas de pods
esbarra rapidamente no limite de capacidade do nó.

## Decisão

**Pods — HorizontalPodAutoscaler** na API e no Web, com as métricas do `metrics-server`
(instalado no boot do k3s):

| Parâmetro | Valor | Motivo |
|---|---|---|
| Réplicas | mínimo 1, máximo 5 | Ver "mínimo de 1" abaixo |
| Alvo de CPU | 70% do request | Folga para absorver o pico enquanto um pod novo sobe |
| Alvo de memória | 80% do request | O .NET aloca memória de forma crescente; escalar antes do limit evita OOMKill |
| Scale up | Sem janela de estabilização | Reagir ao pico imediatamente |
| Scale down | Janela de 120 s | Evitar oscilação quando a carga cai e volta |
| Recursos por pod | request 100m / 192 Mi, limit 500m / 384 Mi | O HPA calcula a utilização sobre o request; sem request não há escala |

**Nós — Auto Scaling Group de workers** k3s, com *target tracking* de CPU média em 60%
(mínimo 1, máximo 3 nós). Os workers entram no cluster no boot, lendo o token de adesão no SSM,
e são registrados automaticamente no target group do ALB interno.

**Mínimo de 1 réplica, de propósito.** Cada pod da API aplica as migrations e o seed ao iniciar.
Com duas réplicas no primeiro boot, os dois pods tentariam inserir o seed ao mesmo tempo, antes
de qualquer um ver o banco populado.

## Alternativas

| Alternativa | Por que não |
|---|---|
| Réplicas fixas | Paga por capacidade ociosa fora do pico e não absorve picos maiores que o previsto |
| HPA por métrica de negócio (requisições por segundo, fila) | Exige Prometheus Adapter ou KEDA; CPU e memória já acompanham a carga de uma API síncrona |
| Cluster Autoscaler | Escala nós quando há pods pendentes, o que é mais preciso que CPU média, mas é mais um componente rodando nos nós `t3.small`, com permissão IAM para alterar o ASG. O *target tracking* resolve com um recurso Terraform e nada rodando no cluster |
| Vertical Pod Autoscaler | Reinicia pods para mudar recursos; não resolve pico e conflita com o HPA na mesma métrica |

## Consequências

- A capacidade acompanha a carga nos dois níveis: primeiro pods (segundos), depois nós
  (minutos, o tempo de boot da EC2 e de adesão ao cluster).
- O ASG reage à CPU média dos nós, não a pods pendentes: se o HPA pedir réplicas que não cabem
  por memória, com CPU ainda abaixo de 60%, os pods ficam pendentes até a CPU subir. Se isso
  aparecer no painel, o Cluster Autoscaler passa a compensar.
- O primeiro boot é sequencial por causa do seed. Se for preciso mínimo de 2 réplicas, as
  migrations e o seed devem sair do startup para um `Job` do Kubernetes executado antes do
  deploy.
- O painel de monitoramento acompanha réplicas atuais e desejadas do HPA, e há alerta de CPU e
  memória acima de 90% do limit ([ADR-010](ADR-010-observabilidade-new-relic.md)).
- O agente APM aumenta o consumo de memória dos pods; o limit de 384 Mi deve ser reavaliado
  depois de ligar o New Relic.
