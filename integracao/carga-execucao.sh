#!/bin/sh
# Usado pelo serviço carga-execucao do docker-compose: o estoque e o catálogo de serviços só
# existem na Execução (o DynamoDB Local daqui sobe vazio). Faz login no MechanicLtda como
# administrador e cadastra alguns itens de exemplo pela API da Execução. Se o estoque já
# tiver itens, não faz nada.
set -eu

resposta=$(curl -sf -X POST "${MONOLITO_URL}/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"${ADMIN_EMAIL}\",\"senha\":\"${ADMIN_SENHA}\"}")
token=$(echo "$resposta" | sed -E 's/.*"token":"([^"]+)".*/\1/')

chamar() {
  curl -sf -H "Authorization: Bearer ${token}" -H 'Content-Type: application/json' "$@"
}

if [ "$(chamar "${EXECUCAO_URL}/api/estoque")" != "[]" ]; then
  echo "Estoque da Execução já tem itens; nada a carregar."
  exit 0
fi

for item in \
  '{"nome":"Óleo Motor 5W30","tipo":"Insumo","quantidadeAtual":50,"quantidadeMinima":10}' \
  '{"nome":"Filtro de Ar","tipo":"Peca","quantidadeAtual":30,"quantidadeMinima":5}' \
  '{"nome":"Pastilha de Freio","tipo":"Peca","quantidadeAtual":20,"quantidadeMinima":4}' \
  '{"nome":"Fluido de Freio DOT 4","tipo":"Insumo","quantidadeAtual":15,"quantidadeMinima":3}' \
  '{"nome":"Correia Dentada","tipo":"Peca","quantidadeAtual":8,"quantidadeMinima":2}'
do
  chamar -X POST "${EXECUCAO_URL}/api/estoque" -d "$item" > /dev/null
done

for servico in \
  '{"nome":"Troca de óleo","descricao":"Mão de obra da troca de óleo e filtro","valorBase":80.00,"ativo":true}' \
  '{"nome":"Revisão de freios","descricao":"Inspeção e troca de pastilhas","valorBase":150.00,"ativo":true}'
do
  chamar -X POST "${EXECUCAO_URL}/api/servicos-oficina" -d "$servico" > /dev/null
done

echo "Carga de exemplo da Execução concluída."
