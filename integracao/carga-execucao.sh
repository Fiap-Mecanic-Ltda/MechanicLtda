#!/usr/bin/env bash
# Usado pelo serviço carga-execucao do docker-compose: faz login no MechanicLtda como
# administrador e roda tools/ExecucaoService.Carga com o token.
set -euo pipefail

resposta=$(curl -sf -X POST "${MONOLITO_URL}/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"${ADMIN_EMAIL}\",\"senha\":\"${ADMIN_SENHA}\"}")

MONOLITO_TOKEN=$(echo "$resposta" | sed -E 's/.*"token":"([^"]+)".*/\1/')
export MONOLITO_TOKEN

# O código vem montado só para leitura; o build precisa de uma cópia gravável.
cp -r /src /tmp/carga
cd /tmp/carga
dotnet run --project tools/ExecucaoService.Carga
