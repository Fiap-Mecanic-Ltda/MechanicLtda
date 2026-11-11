#!/usr/bin/env bash
# Dá acesso de leitura ao soat-architecture em TODOS os repositórios da organização
# (MechanicLtda, Lambda, infra, OS Service, Orçamento e Pagamento e Execução e Produção),
# inclusive os que forem criados depois: a lista vem do GitHub a cada execução.
#
# Pré-requisitos: GitHub CLI autenticado com uma conta ADMIN da organização (gh auth login).
#
#   bash scripts/convidar-soat.sh
set -uo pipefail

ORG="Fiap-Mecanic-Ltda"
USUARIO="soat-architecture"

command -v gh >/dev/null 2>&1 || { echo "Instale o GitHub CLI: https://cli.github.com"; exit 1; }
gh auth status >/dev/null 2>&1 || { echo "Autentique antes: gh auth login"; exit 1; }

repos=$(gh repo list "$ORG" --limit 200 --no-archived --json name --jq '.[].name') || { echo "Nao foi possivel listar os repositorios de $ORG"; exit 1; }
[ -n "$repos" ] || { echo "Nenhum repositorio encontrado em $ORG"; exit 1; }

falhas=0
for repo in $repos; do
  if gh api -X PUT "repos/$ORG/$repo/collaborators/$USUARIO" -f permission=pull >/dev/null 2>/tmp/convite-erro; then
    echo "ok     leitura para $USUARIO em $repo"
  else
    echo "FALHOU $repo -> $(tr '\n' ' ' </tmp/convite-erro)"
    falhas=$((falhas + 1))
  fi
done

if [ "$falhas" -gt 0 ]; then
  echo "$falhas convite(s) falharam."
  exit 1
fi

echo "Convites enviados. O $USUARIO precisa aceitar cada um em https://github.com/notifications (ou pelo e-mail)."
