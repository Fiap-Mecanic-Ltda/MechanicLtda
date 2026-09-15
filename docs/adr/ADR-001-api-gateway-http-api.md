# ADR-001 — API Gateway: AWS API Gateway (HTTP API) com VPC Link e ALB interno

- **Status:** aceito
- **Data:** 2026-09-12
- **Contexto detalhado:** [RFC-001](../rfc/RFC-001-estrategia-api-gateway.md)

## Contexto

A Fase 3 exige um API Gateway para controle e roteamento, e uma Function serverless para
autenticação. Hoje a API é exposta direto pelo IP público da EC2 do k3s, em HTTP.

## Decisão

O ponto único de entrada é um **AWS API Gateway HTTP API**, no stage `$default`, integrado por
**VPC Link** a um **ALB interno** que encaminha para o NodePort `8080` dos nós do k3s. As
funções Lambda de autenticação são integradas ao mesmo gateway.

Detalhes que fazem parte da decisão:

- **Stage `$default`**: em integração privada com stage nomeado, o nome do stage entra no path
  enviado ao backend. Com `$default` o path chega intacto e as rotas da API não mudam.
- **ALB interno em duas AZs**: requisito do próprio ALB, o que implica criar duas sub-redes
  privadas na VPC existente.
- **Backend privado**: o security group dos nós passa a aceitar a porta `8080` apenas do
  security group do ALB. A virada é feita em dois applies para não interromper o ambiente.

## Alternativas

- **REST API**: cache e WAF, porém 3,5 vezes mais caro por requisição e VPC Link apenas para NLB.
- **Kong OSS no cluster**: alinhado às aulas 4 a 6, mas consumiria os mesmos nós `t3.small` da
  aplicação e exigiria gerenciar certificado TLS.
- **Integração HTTP direta ao IP público** (sem VPC Link e sem ALB): mais barato, porém o
  tráfego entre gateway e backend sairia pela internet em HTTP e a porta continuaria aberta.

## Consequências

- Endpoint HTTPS gerenciado, com métricas de latência e access log JSON no CloudWatch.
- Custo fixo do ALB (cerca de US$ 17 a 23 por mês).
- Sem cache no gateway e sem WAF; mitigação por throttling e alarmes.
- A URL do gateway passa a ser a base dos links de aprovação de OS enviados por e-mail.
