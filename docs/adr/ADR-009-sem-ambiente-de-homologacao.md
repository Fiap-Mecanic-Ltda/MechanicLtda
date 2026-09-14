# ADR-009 — Sem ambiente de homologação na AWS; deploy automático só da `main`

- **Status:** aceito
- **Data:** 2026-09-13

## Contexto

O desafio pede "deploy automático das branches de homologação e produção", com `main`
protegida e merge só por Pull Request. O fluxo de trabalho do time é
`feature/*` → `desenvolvimento` → `homologacao` → `main`.

Um ambiente de homologação completo na AWS significaria duplicar a infraestrutura: EC2 do k3s e
workers, RDS, ALB, API Gateway e Lambdas — na ordem de US$ 60 a 80 por mês a mais, praticamente
dobrando o custo do projeto. A alternativa mais barata, homologação no mesmo cluster e na mesma
instância RDS, divide CPU e banco com produção e exige porta, target group e gateway próprios.

## Decisão

**Não há ambiente de homologação na AWS.** O que cada branch dispara:

| Branch | Aplicação | Terraform (InfraKubernete, InfraSGBD, Lambda, observabilidade) |
|---|---|---|
| PR para qualquer branch de trabalho | Build e testes | `plan` |
| Push em `desenvolvimento` ou `homologacao`/`homolog` | Build e testes | `plan` |
| Push em `main` | Build, testes, publicação no ECR e deploy no cluster | `plan` e **apply automático** |

Salvaguardas:

- todo apply roda no GitHub Environment `production`, onde revisores obrigatórios aprovam depois
  de ver o `plan` do mesmo run;
- o apply manual (`workflow_dispatch`) só é aceito a partir da `main`;
- `concurrency` impede dois applies simultâneos sobre o mesmo state;
- a Lambda roda um smoke test do gateway logo após o apply;
- a `main` é protegida: PR aprovado, sem push direto, inclusive de administradores
  (`scripts/proteger-branches.sh`).

## Alternativas

| Alternativa | Por que não |
|---|---|
| Stack completa por ambiente | Isolamento total, mas dobra o custo do projeto |
| Namespace de homologação no mesmo cluster e RDS | Custo baixo, mas uma carga de teste afeta produção e a infraestrutura de entrada precisa ser duplicada (porta, target group, gateway) |
| Deploy de `homologacao` direto em produção | Publicaria código ainda não aprovado para produção |

## Consequências

- **Desvio consciente do requisito**: a branch de homologação é validada (build, testes e
  `plan`), mas não é implantada. A validação de comportamento em nuvem acontece em produção,
  protegida pelo gate do Environment e pelo smoke test.
- A branch de homologação continua útil como etapa de integração e revisão antes da `main`.
- **Para ativar homologação no futuro**, o Terraform já é parametrizado por `environment`: basta
  uma chave de state por ambiente (`homolog/...`), variáveis do ambiente no GitHub e o
  gatilho de apply na branch `homologacao`.
