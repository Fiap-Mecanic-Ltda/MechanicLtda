# Documentação da arquitetura — MechanicLtda

Documentação exigida pelo Tech Challenge da Fase 3 (13SOAT). Ela fica no repositório da
aplicação por ser o ponto de entrada do projeto; os outros três repositórios descrevem, nos
próprios READMEs, a parte da arquitetura que implementam.

Os diagramas usam Mermaid e são renderizados pelo GitHub.

## Onde está cada item pedido

| Item do desafio | Documento |
|---|---|
| Diagrama de componentes (nuvem, APIs, banco e monitoramento) | [01 — Diagrama de componentes](01-diagrama-de-componentes.md) |
| Diagrama de sequência da autenticação e da abertura de OS | [02 — Diagramas de sequência](02-diagramas-de-sequencia.md) |
| Justificativa do banco, ajustes no modelo relacional, diagrama ER e relacionamentos | [03 — Banco de dados](03-banco-de-dados.md) |
| RFCs de decisões técnicas relevantes | [RFCs](#rfcs) |
| ADRs de decisões arquiteturais permanentes | [ADRs](#adrs) |

## RFCs

Discussão das decisões técnicas: contexto, critérios, alternativas comparadas e decisão.

| RFC | Decisão |
|---|---|
| [RFC-001](../rfc/RFC-001-estrategia-api-gateway.md) | API Gateway: AWS HTTP API com VPC Link e ALB interno, em vez de REST API, Kong ou Azure APIM |
| [RFC-002](../rfc/RFC-002-escolha-da-nuvem.md) | Nuvem: manter a AWS, em vez de migrar para Azure ou Google Cloud |
| [RFC-003](../rfc/RFC-003-escolha-do-banco-de-dados.md) | Banco: SQL Server Express no RDS, em vez de SQL Server Standard, PostgreSQL ou DynamoDB |
| [RFC-004](../rfc/RFC-004-estrategia-de-autenticacao.md) | Autenticação: Lambda própria emitindo JWT por CPF e Lambda authorizer, em vez de Cognito ou JWT RS256 com authorizer nativo — com análise de segurança |

## ADRs

Registro curto das decisões permanentes: contexto, decisão e consequências.

| ADR | Decisão |
|---|---|
| [ADR-001](../adr/ADR-001-api-gateway-http-api.md) | API Gateway HTTP API com VPC Link e ALB interno |
| [ADR-002](../adr/ADR-002-autorizacao-em-duas-camadas.md) | Autorização em duas camadas: o gateway autentica, a API valida a posse |
| [ADR-003](../adr/ADR-003-indice-cego-cpf.md) | Índice cego (HMAC) para localizar o cliente pelo CPF cifrado |
| [ADR-004](../adr/ADR-004-segredos-da-lambda.md) | Segredos da Lambda por variável de ambiente, sem NAT nem VPC endpoint |
| [ADR-005](../adr/ADR-005-comunicacao-sincrona-rest.md) | Padrão de comunicação: síncrono por HTTP/REST, sem mensageria |
| [ADR-006](../adr/ADR-006-escalabilidade-hpa-e-asg.md) | Escalabilidade: HPA nos pods e Auto Scaling Group nos nós |
| [ADR-007](../adr/ADR-007-k3s-em-ec2.md) | Kubernetes com k3s em EC2, em vez de EKS |
| [ADR-008](../adr/ADR-008-quatro-repositorios.md) | Quatro repositórios com contratos explícitos |
| [ADR-009](../adr/ADR-009-sem-ambiente-de-homologacao.md) | Sem ambiente de homologação na AWS; deploy automático só da `main` |
| [ADR-010](../adr/ADR-010-observabilidade-new-relic.md) | Observabilidade com New Relic, e CloudWatch na borda serverless |
| [ADR-011](../adr/ADR-011-sql-server-gerenciado-rds.md) | Banco relacional gerenciado: SQL Server Express no RDS |

## Outros documentos

- [Plano de desenvolvimento do API Gateway](../api-gateway/plano-api-gateway.md) — fases, rotas,
  contrato do token, riscos e custos.
