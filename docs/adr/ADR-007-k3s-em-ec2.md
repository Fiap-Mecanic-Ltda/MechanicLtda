# ADR-007 — Kubernetes com k3s em EC2, em vez de EKS

- **Status:** aceito (decisão da Fase 2, formalizada na Fase 3)
- **Data:** 2026-09-13
- **Implementação:** repositório InfraKubernete — `infra/ec2.tf`, `infra/asg.tf`, `infra/templates/`

## Contexto

O desafio exige um cluster Kubernetes com escalabilidade, provisionado por Terraform. A carga do
projeto cabe em poucos nós pequenos, e o orçamento é o de um projeto acadêmico.

## Decisão

Rodar **k3s** — distribuição Kubernetes certificada da CNCF — numa EC2 `t3.small` que atua como
server, com workers num Auto Scaling Group, tudo instalado por `user_data` no boot:

- os workers entram no cluster com o token de adesão publicado no SSM Parameter Store;
- o `metrics-server` é instalado no boot, para o HPA ([ADR-006](ADR-006-escalabilidade-hpa-e-asg.md));
- a API do Kubernetes (porta 6443) **nunca é exposta**: o deploy envia os manifestos por
  SSM Run Command e roda `kubectl apply` dentro da instância;
- a credencial do ECR, que expira em 12 horas, é renovada por um timer a cada 6 horas.

## Alternativas

| Alternativa | Custo e esforço | Por que não |
|---|---|---|
| **Amazon EKS** | Control plane a US$ 0,10 por hora (cerca de US$ 73 por mês) além dos nós | Mais que o dobro do custo de computação atual só pelo control plane, para uma carga de poucos pods |
| EKS com Fargate | Sem nós para gerenciar; cobrança por pod | Mesmo custo de control plane, e o HPA passa a depender da latência de provisionamento do Fargate |
| ECS Fargate (Serverless, aula 4) | Sem Kubernetes | Não atende ao requisito de cluster Kubernetes |
| Minikube ou kind numa EC2 | Gratuito | Feitos para desenvolvimento local, sem suporte a nós adicionais em produção |

## Consequências

- Nenhum custo além das EC2: o control plane roda no próprio server.
- O time opera o que o EKS gerenciaria: atualização do k3s, integração com ECR e credenciais.
- O server é ponto único de falha do control plane: se ele cair, os pods em execução nos
  workers continuam atendendo, mas não há novos agendamentos até ele voltar. Aceitável para o
  escopo.
- Os manifestos são Kubernetes padrão (Kustomize): migrar para EKS troca a infraestrutura em
  `infra/`, não os manifestos em `k8s/`.
