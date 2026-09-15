#!/usr/bin/env bash
# Exercita a aplicação pelo API Gateway até que cada painel, evento e alerta da
# observabilidade tenha dado para mostrar. É o complemento do smoke test do
# repositório Lambda: lá se valida o caminho (gateway, authorizer, posse); aqui
# se valida o que o monitoramento enxerga.
#
# Uso:
#   ADMIN_SENHA='...' bash scripts/validar-observabilidade.sh https://ID.execute-api.us-east-1.amazonaws.com
#
# Variáveis de ambiente:
#   ADMIN_SENHA   senha do administrador (obrigatória)
#   ADMIN_EMAIL   e-mail do administrador (padrão: admin@mechanic.com)
#   CPF_CLIENTE   CPF de um cliente ativo; habilita os passos do token por CPF
#
# Opções:
#   --ciclos N    ordens de serviço completas a executar (padrão: 2)
#   --pausa S     segundos entre as transições de status (padrão: 15)
#   --carga S     segundos de carga paralela para acionar o HPA (padrão: 0, pulado)
#   --sem-falhas  não gera as falhas de negócio propositais
#
# ATENÇÃO: o script cria ordens de serviço de verdade no ambiente apontado e
# consome itens do estoque. Use no ambiente de demonstração.

set -uo pipefail

BASE="${1:?Informe a URL base do gateway (output api_base_url do Terraform da Lambda)}"
BASE="${BASE%/}"
shift

CICLOS=2
PAUSA=15
CARGA=0
GERAR_FALHAS=1

while [ $# -gt 0 ]; do
  case "$1" in
    --ciclos) CICLOS="$2"; shift 2 ;;
    --pausa) PAUSA="$2"; shift 2 ;;
    --carga) CARGA="$2"; shift 2 ;;
    --sem-falhas) GERAR_FALHAS=0; shift ;;
    *) echo "Opção desconhecida: $1"; exit 2 ;;
  esac
done

ADMIN_EMAIL="${ADMIN_EMAIL:-admin@mechanic.com}"
ADMIN_SENHA="${ADMIN_SENHA:?Defina ADMIN_SENHA com a senha do administrador}"
CPF_CLIENTE="${CPF_CLIENTE:-}"

PYTHON=$(command -v python3 || command -v python || true)
[ -n "$PYTHON" ] || { echo "python3 (ou python) é necessário para ler as respostas JSON."; exit 2; }
command -v curl >/dev/null || { echo "curl é necessário."; exit 2; }

CABECALHOS=$(mktemp)
CORPO=""
STATUS=""
FALHAS=0
CORRELACOES=()

trap 'rm -f "$CABECALHOS"' EXIT

# ── Utilitários ──────────────────────────────────────────────────────────────

campo() {
  # Lê um caminho simples do JSON recebido por stdin. Aceita índice de lista:
  # campo "cliente.id" ou campo "0.id".
  "$PYTHON" -c '
import json, sys
try:
    dado = json.load(sys.stdin)
except ValueError:
    sys.exit(1)
for parte in sys.argv[1].split("."):
    if parte == "":
        continue
    try:
        dado = dado[int(parte)] if parte.lstrip("-").isdigit() else dado[parte]
    except (KeyError, IndexError, TypeError):
        sys.exit(1)
if dado is None:
    sys.exit(1)
print(dado)
' "$1" 2>/dev/null
}

requisicao() {
  # requisicao MÉTODO CAMINHO [TOKEN] [CORPO_JSON]
  local metodo="$1" caminho="$2" token="${3:-}" corpo="${4:-}"
  local args=(-s -m 60 -X "$metodo" -D "$CABECALHOS" -o -)

  [ -n "$token" ] && args+=(-H "Authorization: Bearer $token")
  if [ -n "$corpo" ]; then
    args+=(-H "Content-Type: application/json" -d "$corpo")
  fi

  local resposta
  resposta=$(curl "${args[@]}" -w $'\n%{http_code}' "$BASE$caminho")
  STATUS="${resposta##*$'\n'}"
  CORPO="${resposta%$'\n'*}"

  local correlacao
  correlacao=$(grep -i '^x-correlation-id:' "$CABECALHOS" | tail -1 | tr -d '\r' | cut -d' ' -f2-)
  [ -n "$correlacao" ] && CORRELACOES+=("$metodo $caminho -> $correlacao")
}

verificar() {
  # verificar DESCRIÇÃO ESPERADO OBTIDO
  if [ "$2" = "$3" ]; then
    echo "  ok     $1 ($3)"
  else
    echo "  FALHOU $1: esperado $2, obtido $3"
    [ -n "$CORPO" ] && echo "         ${CORPO:0:200}"
    FALHAS=$((FALHAS + 1))
  fi
}

titulo() {
  echo
  echo "── $1"
}

# ── 1. Caminho até a aplicação ───────────────────────────────────────────────

titulo "1. Gateway, VPC Link, ALB e pods  [painel: APIs > Disponibilidade de /health]"
requisicao GET /health
verificar "GET /health" 200 "$STATUS"

if [ "$STATUS" != "200" ]; then
  echo
  echo "A aplicação não respondeu pelo gateway. Sem isso nada mais faz sentido; parando."
  exit 1
fi

# ── 2. Autenticação ──────────────────────────────────────────────────────────

titulo "2. Autenticação  [APM: transações de /auth]"
requisicao POST /auth/login "" "{\"email\":\"$ADMIN_EMAIL\",\"senha\":\"$ADMIN_SENHA\"}"
verificar "POST /auth/login (funcionário)" 200 "$STATUS"
TOKEN_ADMIN=$(printf '%s' "$CORPO" | campo token)
[ -n "$TOKEN_ADMIN" ] || { echo "Sem token de administrador; parando."; exit 1; }

TOKEN_CLIENTE=""
CLIENTE_ID=""
if [ -n "$CPF_CLIENTE" ]; then
  requisicao POST /auth/cpf "" "{\"cpf\":\"$CPF_CLIENTE\"}"
  verificar "POST /auth/cpf (cliente)" 200 "$STATUS"
  TOKEN_CLIENTE=$(printf '%s' "$CORPO" | campo token)
  CLIENTE_ID=$(printf '%s' "$CORPO" | campo cliente.id)
else
  echo "  (CPF_CLIENTE não definido: os passos com token de cliente serão pulados)"
fi

if [ -z "$CLIENTE_ID" ]; then
  requisicao GET /api/cliente "$TOKEN_ADMIN"
  CLIENTE_ID=$(printf '%s' "$CORPO" | campo 0.id)
  [ -n "$CLIENTE_ID" ] || { echo "Nenhum cliente cadastrado; parando."; exit 1; }
  echo "  usando o cliente $CLIENTE_ID para as ordens de serviço"
fi

# ── 3. Dados de apoio ────────────────────────────────────────────────────────

titulo "3. Veículo e item de estoque"
requisicao GET "/api/veiculo/cliente/$CLIENTE_ID" "$TOKEN_ADMIN"
VEICULO_ID=$(printf '%s' "$CORPO" | campo 0.id)
verificar "GET /api/veiculo/cliente/$CLIENTE_ID" 200 "$STATUS"
[ -n "$VEICULO_ID" ] || { echo "O cliente $CLIENTE_ID não tem veículo cadastrado; parando."; exit 1; }

requisicao GET /api/estoque "$TOKEN_ADMIN"
ESTOQUE_ID=$(printf '%s' "$CORPO" | "$PYTHON" -c '
import json, sys
itens = [i for i in json.load(sys.stdin) if i.get("quantidadeAtual", 0) > 0]
print(itens[0]["id"] if itens else "")
' 2>/dev/null)
verificar "GET /api/estoque" 200 "$STATUS"
[ -n "$ESTOQUE_ID" ] || { echo "Nenhum item de estoque com saldo; parando."; exit 1; }

# ── 4. Ciclos completos de ordem de serviço ──────────────────────────────────

titulo "4. $CICLOS ordem(ns) de serviço de ponta a ponta  [painel: Ordens de servico]"
echo "  cada transição gera um OrdemServicoEvento com o tempo gasto no status anterior"

ORDENS=()
for ciclo in $(seq 1 "$CICLOS"); do
  echo "  ciclo $ciclo/$CICLOS"

  requisicao POST /api/ordemservico "$TOKEN_ADMIN" \
    "{\"descricaoProblema\":\"Validacao da observabilidade - ciclo $ciclo\",\"valorTotalEstimado\":350.00,\"veiculoId\":$VEICULO_ID,\"clienteId\":$CLIENTE_ID}"
  verificar "    criar OS" 200 "$STATUS"
  OS_ID=$(printf '%s' "$CORPO" | campo id)
  [ -n "$OS_ID" ] || continue
  ORDENS+=("$OS_ID")

  if [ "$GERAR_FALHAS" = "1" ]; then
    # Transição inválida de propósito: a OS está em "Recebida" e finalizar exige
    # "Em Execução". Vira OrdemServicoFalha do tipo Negocio.
    requisicao PATCH "/api/ordemservico/$OS_ID/finalizar" "$TOKEN_ADMIN"
    verificar "    transição inválida (falha de negócio proposital)" 400 "$STATUS"
  fi

  sleep "$PAUSA"
  requisicao PATCH "/api/ordemservico/$OS_ID/iniciar-diagnostico" "$TOKEN_ADMIN"
  verificar "    iniciar diagnóstico" 200 "$STATUS"

  requisicao POST "/api/ordem-servico/$OS_ID/itens" "$TOKEN_ADMIN" \
    "{\"estoqueId\":$ESTOQUE_ID,\"quantidade\":1,\"valorUnitario\":120.00}"
  verificar "    adicionar item" 200 "$STATUS"

  sleep "$PAUSA"
  requisicao PATCH "/api/ordemservico/$OS_ID/aguardar-aprovacao" "$TOKEN_ADMIN"
  verificar "    aguardar aprovação (dispara os e-mails)" 200 "$STATUS"

  sleep "$PAUSA"
  requisicao PATCH "/api/ordemservico/$OS_ID/aprovar" "$TOKEN_ADMIN"
  verificar "    aprovar" 200 "$STATUS"

  sleep "$PAUSA"
  requisicao PATCH "/api/ordemservico/$OS_ID/finalizar" "$TOKEN_ADMIN"
  verificar "    finalizar" 200 "$STATUS"

  sleep "$PAUSA"
  requisicao PATCH "/api/ordemservico/$OS_ID/entregar" "$TOKEN_ADMIN"
  verificar "    entregar" 200 "$STATUS"
done

# ── 5. Falhas de negócio isoladas ────────────────────────────────────────────

if [ "$GERAR_FALHAS" = "1" ]; then
  titulo "5. Falhas propositais  [painel: Falhas no processamento de OS e Erros por tipo de excecao]"
  echo "  só transições registram OrdemServicoFalha: a consulta por id trata o erro"
  echo "  na camada de aplicação, antes do monitoramento"

  requisicao PATCH "/api/ordemservico/99999999/iniciar-diagnostico" "$TOKEN_ADMIN"
  verificar "iniciar diagnóstico de OS inexistente" 400 "$STATUS"

  requisicao PATCH "/api/ordemservico/99999999/aprovar" "$TOKEN_ADMIN"
  verificar "aprovar OS inexistente" 400 "$STATUS"
fi

# ── 6. Autorização em duas camadas ───────────────────────────────────────────

if [ -n "$TOKEN_CLIENTE" ]; then
  titulo "6. Authorizer e posse do recurso  [CloudWatch: access log do gateway]"

  requisicao GET "/api/ordemservico/cliente/$CLIENTE_ID" "$TOKEN_CLIENTE"
  verificar "cliente consulta as próprias OS" 200 "$STATUS"

  requisicao GET "/api/ordemservico/cliente/$((CLIENTE_ID + 100000))" "$TOKEN_CLIENTE"
  verificar "cliente tenta ver outro cliente (API barra)" 403 "$STATUS"

  requisicao GET /api/cliente "$TOKEN_CLIENTE"
  verificar "cliente em rota administrativa (authorizer barra)" 403 "$STATUS"

  requisicao GET "/api/ordemservico/cliente/$CLIENTE_ID"
  verificar "sem token" 401 "$STATUS"
fi

# ── 7. Carga para o HPA ──────────────────────────────────────────────────────

if [ "$CARGA" -gt 0 ]; then
  titulo "7. Carga por ${CARGA}s  [painel: Kubernetes > Replicas do HPA e CPU por pod]"
  echo "  o HPA leva 1 a 2 minutos para reagir; o painel atualiza logo depois"

  fim=$(( $(date +%s) + CARGA ))
  export BASE TOKEN_ADMIN
  while [ "$(date +%s)" -lt "$fim" ]; do
    seq 1 8 | xargs -P 8 -I{} curl -s -o /dev/null -m 20 \
      -H "Authorization: Bearer $TOKEN_ADMIN" "$BASE/api/ordemservico"
  done
  echo "  carga concluída"
fi

# ── Resumo ───────────────────────────────────────────────────────────────────

echo
echo "════════════════════════════════════════════════════════════════════"
if [ "$FALHAS" -gt 0 ]; then
  echo "$FALHAS verificação(ões) falharam."
else
  echo "Todas as verificações passaram."
fi
[ ${#ORDENS[@]} -gt 0 ] && echo "Ordens de serviço criadas: ${ORDENS[*]}"

echo
echo "Correlação para achar estas requisições no New Relic (últimas 5):"
for ((i = ${#CORRELACOES[@]} - 1, n = 0; i >= 0 && n < 5; i--, n++)); do
  echo "  ${CORRELACOES[$i]}"
done

cat <<'CONSULTAS'

Consultas para rodar no Query builder do New Relic:

  -- Eventos de negócio gerados agora
  SELECT count(*) FROM OrdemServicoEvento FACET tipo SINCE 30 minutes ago

  -- Tempo em cada etapa, que alimenta o painel de ordens de serviço
  SELECT average(minutosNoStatusAnterior) FROM OrdemServicoEvento
  WHERE tipo = 'MudancaStatus' FACET statusAnteriorDescricao SINCE 30 minutes ago

  -- Falhas propositais
  SELECT count(*) FROM OrdemServicoFalha FACET classificacao, operacao SINCE 30 minutes ago

  -- Uma requisição inteira pelo id de correlação (troque o valor)
  SELECT timestamp, level, message FROM Log
  WHERE context.CorrelationId = 'COLE_AQUI' SINCE 30 minutes ago

  -- Latência e throughput da API
  SELECT percentile(duration, 50, 95, 99) FROM Transaction
  WHERE appName = 'mechanicltda-api' SINCE 30 minutes ago TIMESERIES

Painel: Dashboards > "MechanicLtda - mechanicltda-prod"
Alertas: Alerts > Alert policies > "mechanicltda-prod - MechanicLtda"
CONSULTAS

[ "$FALHAS" -gt 0 ] && exit 1
exit 0
