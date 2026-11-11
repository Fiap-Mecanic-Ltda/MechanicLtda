#!/usr/bin/env bash
# Aplica a proteção de branches exigida pelo Tech Challenge nos repositórios
# do projeto (inclusive os três do OS Service da Fase 4): nada de commit direto, merge só por Pull Request
# aprovado e, onde o CI roda em todo PR, checks obrigatórios.
#
# Pré-requisitos: GitHub CLI instalado e autenticado com uma conta ADMIN da
# organização (gh auth login). Em repositórios privados de organização no plano
# Free, o GitHub recusa branch protection (HTTP 403) — o script avisa e segue.
#
#   bash scripts/proteger-branches.sh                  # aplica
#   APROVACOES=2 bash scripts/proteger-branches.sh     # exige 2 aprovações
#   bash scripts/proteger-branches.sh --convidar-soat  # também roda scripts/convidar-soat.sh
#                                                      (soat-architecture com leitura em todos os repositórios)
set -uo pipefail

ORG="Fiap-Mecanic-Ltda"
APROVACOES="${APROVACOES:-1}"
CONVIDAR_SOAT=false
[ "${1:-}" = "--convidar-soat" ] && CONVIDAR_SOAT=true

command -v gh >/dev/null 2>&1 || { echo "Instale o GitHub CLI: https://cli.github.com"; exit 1; }
gh auth status >/dev/null 2>&1 || { echo "Autentique antes: gh auth login"; exit 1; }

falhas=0

# proteger <repositorio> <branch> [check obrigatório...]
#
# Os checks são os NOMES dos jobs dos workflows. Só entram onde o workflow roda
# em todo PR: exigir um check de workflow com filtro de paths (os Terraform de
# InfraKubernete e InfraSGBD) travaria para sempre os PRs que não mexem naqueles
# arquivos, porque o check nunca apareceria.
proteger() {
  local repo="$1" branch="$2"
  shift 2

  local checks="null"
  if [ "$#" -gt 0 ]; then
    local contexts
    contexts=$(printf '"%s",' "$@")
    checks="{\"strict\": true, \"contexts\": [${contexts%,}]}"
  fi

  local corpo
  corpo=$(cat <<JSON
{
  "required_status_checks": $checks,
  "enforce_admins": true,
  "required_pull_request_reviews": {
    "required_approving_review_count": $APROVACOES,
    "dismiss_stale_reviews": true
  },
  "restrictions": null,
  "allow_force_pushes": false,
  "allow_deletions": false,
  "required_conversation_resolution": true
}
JSON
)

  if printf '%s' "$corpo" | gh api -X PUT "repos/$ORG/$repo/branches/$branch/protection" --input - >/dev/null 2>/tmp/protecao-erro; then
    echo "ok     $repo:$branch${*:+ (checks: $*)}"
  else
    echo "FALHOU $repo:$branch -> $(tr '\n' ' ' </tmp/protecao-erro)"
    falhas=$((falhas + 1))
  fi
}

# enforce_admins = true: nem administradores fazem push direto — é o que o
# desafio pede ("sem commits diretos").

proteger MechanicLtda   main            "Build e testes"
proteger MechanicLtda   homologacao     "Build e testes"
proteger MechanicLtda   desenvolvimento "Build e testes"

proteger Lambda         main            "Build, testes e empacotamento" "Terraform plan"
proteger Lambda         homologacao     "Build, testes e empacotamento" "Terraform plan"

proteger InfraKubernete main
proteger InfraKubernete homolog

proteger InfraSGBD      main
proteger InfraSGBD      homologacao

# Fase 4: OS Service. "Build da imagem Docker" só roda em PR (no push da main o
# job é o de publicação no ECR), então é exigido como check do PR.
proteger OrdemServico-service         main            "Build e testes" "Build da imagem Docker"
proteger OrdemServico-service         homologacao     "Build e testes" "Build da imagem Docker"
proteger OrdemServico-service         desenvolvimento "Build e testes" "Build da imagem Docker"

proteger OrdemServico-InfraKubernete  main
proteger OrdemServico-InfraKubernete  homologacao

proteger OrdemServico-InfraSGBD       main
proteger OrdemServico-InfraSGBD       homologacao

if [ "$CONVIDAR_SOAT" = true ]; then
  bash "$(dirname "$0")/convidar-soat.sh" || falhas=$((falhas + 1))
fi

if [ "$falhas" -gt 0 ]; then
  echo "$falhas operacao(oes) falharam. Em repositorio privado no plano Free, torne-o publico ou use GitHub Team para ter branch protection."
  exit 1
fi

echo "Protecao aplicada."
