# RFC-002 — Escolha do provedor de nuvem

| Campo | Valor |
|---|---|
| Status | Aceito |
| Data | 2026-09-13 |
| Autores | Time MechanicLtda (13SOAT) |
| Fase | Tech Challenge — Fase 3 |
| Decisão registrada em | [ADR-001](../adr/ADR-001-api-gateway-http-api.md), [ADR-007](../adr/ADR-007-k3s-em-ec2.md), [ADR-011](../adr/ADR-011-sql-server-gerenciado-rds.md) |

## 1. Contexto

O desafio deixa a nuvem a critério do time ("livre escolha de nuvem") e exige, nela: API
Gateway, Function serverless de autenticação, banco de dados gerenciado, cluster Kubernetes com
escalabilidade e Terraform para provisionar tudo.

A Fase 2 já entregou uma plataforma na **AWS**, toda em Terraform:

- VPC com sub-redes pública e de banco, security groups e IAM;
- Kubernetes (k3s) numa EC2, com workers em Auto Scaling Group e HPA nos pods;
- RDS SQL Server privado;
- ECR para as imagens, SSM Parameter Store para os segredos;
- state remoto em S3 e pipelines do GitHub Actions autenticando por OIDC.

A pergunta desta RFC é se a Fase 3 continua na AWS ou se migra, e por quê.

## 2. Critérios

1. **Cobertura dos requisitos** com serviços gerenciados.
2. **Custo de migração** dentro do prazo da fase.
3. **Custo de operação** num projeto acadêmico com tráfego baixo.
4. **Aderência à stack** (.NET 9, SQL Server, Terraform, GitHub Actions).
5. **Aderência ao material da fase**, para que as decisões possam ser defendidas com ele.

## 3. Alternativas

| Critério | **AWS (manter)** | Azure | Google Cloud |
|---|---|---|---|
| API Gateway | API Gateway (HTTP API e REST API) | API Management | API Gateway / Apigee |
| Function serverless | Lambda (runtime .NET 8 gerenciado) | Azure Functions (.NET isolado) | Cloud Run functions |
| Banco gerenciado | RDS SQL Server (Express a Enterprise) | Azure SQL Database — SQL Server nativo como PaaS | Cloud SQL for SQL Server |
| Kubernetes | EKS, ou k3s em EC2 (atual) | AKS | GKE |
| Terraform | Provider `aws` já usado nos quatro repositórios | Provider `azurerm` | Provider `google` |
| Migração | **Nenhuma** | Reescrever toda a infraestrutura e o CI/CD; migrar dados | Idem |
| Material da fase | Desenvolvimento Serverless, aula 4 (API Gateway + VPC Link + ALB) | API Gateway, aulas 2 e 3 (Azure APIM) | — |

### Onde o Azure seria melhor

Vale registrar com honestidade: para uma aplicação .NET com SQL Server, o Azure tem vantagens
reais. O **Azure SQL Database** é SQL Server como serviço, sem o acréscimo de licença que o RDS
cobra na hora, e tem camada serverless; o **APIM** é o gateway estudado nas aulas 2 e 3; e o AKS
não cobra pelo control plane no tier gratuito.

### Por que isso não compensa agora

Nenhuma dessas vantagens atende a um requisito que a AWS não atenda. E todas custam a
reescrita de três dos quatro repositórios — VPC, cluster, banco, IAM/OIDC, states e pipelines —
mais a migração de dados, no prazo de uma fase cujo objetivo é acrescentar gateway,
autenticação serverless e observabilidade, e não refazer a base.

## 4. Decisão

**Manter a AWS** como provedor único.

| Requisito do desafio | Serviço AWS | Onde está |
|---|---|---|
| API Gateway | API Gateway HTTP API + VPC Link + ALB interno | Lambda (gateway) e InfraKubernete (ALB) |
| Function serverless de autenticação | Lambda .NET 8 (autenticação e authorizer) | Lambda |
| Banco gerenciado | RDS SQL Server Express | InfraSGBD |
| Kubernetes com escalabilidade | k3s em EC2, ASG de workers, HPA | InfraKubernete |
| Terraform | Provider `aws`, state em S3 com lock nativo | Os quatro repositórios |

## 5. Consequências

- A Fase 3 soma recursos à infraestrutura existente, sem migração: sub-redes de aplicação, ALB,
  VPC Link, HTTP API, duas Lambdas e alarmes.
- O time fica dependente de serviços proprietários da AWS (API Gateway, Lambda, SSM). O código
  de domínio continua portável: a aplicação só conhece HTTP, SQL Server e variáveis de ambiente.
- A observabilidade não usa a ferramenta nativa da nuvem como principal: o desafio pede Datadog
  ou New Relic, e o time escolheu New Relic ([ADR-010](../adr/ADR-010-observabilidade-new-relic.md)).
- O custo com licença do SQL Server no RDS é aceito; a alternativa dentro da AWS é discutida na
  [RFC-003](RFC-003-escolha-do-banco-de-dados.md).

## Referências

- 13SOAT — Fase 3 — Tech Challenge (requisitos de infraestrutura).
- POSTECH Software Architecture, Fase 3 — Desenvolvimento Serverless, aula 4.
- POSTECH Software Architecture, Fase 3 — API Gateway, aulas 1 a 6.
