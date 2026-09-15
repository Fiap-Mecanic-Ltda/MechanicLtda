# ADR-008 — Quatro repositórios com contratos explícitos entre si

- **Status:** aceito
- **Data:** 2026-09-13

## Contexto

O desafio exige a aplicação segregada em quatro repositórios, cada um com CI/CD e deploy
automático: Lambda, infraestrutura Kubernetes, infraestrutura do banco gerenciado e aplicação.
Até a Fase 2 tudo vivia no repositório da aplicação, e um único workflow fazia Terraform, build e
deploy. Separar sem definir como os repositórios conversam só troca um acoplamento visível por
um acoplamento escondido.

## Decisão

| Repositório | Responsabilidade | Publica | Consome |
|---|---|---|---|
| **MechanicLtda** | Aplicação .NET 9 (API e Web), testes, Dockerfiles | Imagens no ECR (tag = SHA do commit) | — |
| **InfraKubernete** | VPC, EC2/k3s, ASG, ALB interno, ECR, SSM, IAM/OIDC, manifestos `k8s/` e observabilidade | Outputs no state (`vpc_id`, sub-redes, `alb_listener_arn`, security groups) e segredos no SSM | Imagens do ECR |
| **InfraSGBD** | RDS SQL Server | Output `rds_security_group_id` e connection string no SSM | Rede e security groups do InfraKubernete |
| **Lambda** | Funções de autenticação, API Gateway e VPC Link | Output `api_base_url` | Rede e ALB do InfraKubernete, security group do RDS e segredos do SSM |

**Os contratos entre os repositórios são quatro, e só quatro:**

1. **ECR** — a aplicação publica, o InfraKubernete implanta.
2. **`terraform_remote_state`** — cada stack lê os outputs dos stacks de que depende, no mesmo
   bucket S3 e com chaves separadas (`prod/terraform.tfstate`, `prod/sgbd/terraform.tfstate`,
   `prod/lambda/terraform.tfstate`, `prod/observability/terraform.tfstate`). Nenhum stack
   descobre recursos de outro por nome ou tag.
3. **SSM Parameter Store** — segredos compartilhados (chave JWT, chave do hash do CPF, connection
   string) têm um dono, que grava, e leitores que só leem.
4. **`repository_dispatch`** — a aplicação avisa o InfraKubernete que há imagem nova, com a tag
   no payload.

**Dependência circular resolvida por quem conhece quem.** A regra de entrada na porta 1433 para a
Lambda é criada no repositório da Lambda, e não no InfraSGBD: o banco não precisa saber que a
Lambda existe. Pelo mesmo motivo, o ALB aceita o CIDR das sub-redes de aplicação, e não o
security group do VPC Link.

## Consequências

- Cada repositório tem pipeline, permissões e ciclo de deploy próprios; mudar a Lambda não
  replaneja o cluster.
- **Há ordem de subida**: InfraKubernete, InfraSGBD, aplicação, Lambda. Um stack não aplica antes
  de os outputs que ele lê existirem. A ordem está documentada no README de cada repositório.
- Mudanças de contrato (renomear um output ou um parâmetro do SSM) quebram outro repositório em
  silêncio até o próximo `plan` dele. Outputs e parâmetros são tratados como API pública: são
  adicionados, e não renomeados.
- A documentação da arquitetura, as RFCs e as ADRs ficam no repositório da aplicação, que é o
  ponto de entrada do projeto — criar um quinto repositório só para documentos fugiria da
  estrutura pedida.
