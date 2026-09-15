# Coleção Postman — Fase 3

Coleção das rotas publicadas no **API Gateway**: autenticação por CPF e por e-mail, rotas
protegidas pelo Lambda authorizer e o fluxo completo da ordem de serviço.

| Arquivo | Conteúdo |
|---|---|
| [MechanicLtda-Fase3.postman_collection.json](MechanicLtda-Fase3.postman_collection.json) | 36 requisições em 5 pastas, com testes automatizados |
| [MechanicLtda-Fase3.postman_environment.json](MechanicLtda-Fase3.postman_environment.json) | Ambiente de produção (URL do gateway, CPFs e usuário do seed) |

## Como usar

1. No Postman, **Import** e selecione os dois arquivos.
2. Selecione o ambiente **MechanicLtda · Produção (AWS)** e preencha:
   - `baseUrl`: output `api_base_url` do Terraform do repositório
     [Lambda](https://github.com/Fiap-Mecanic-Ltda/Lambda), sem barra no final
     (`terraform output -raw api_base_url`);
   - `senhaAdmin`: senha do `admin@mechanic.com` (variável do tipo *secret*, não aparece em tela);
   - `emailDemonstracao` (opcional): caixa que vai receber o link de aprovação da OS.
3. Execute as pastas na ordem. Os testes guardam tokens e ids nas variáveis da coleção, então a
   coleção inteira também roda pelo **Collection Runner**.

## O que cada pasta demonstra

| Pasta | Rotas | Resultado esperado |
|---|---|---|
| 00 · Saúde e documentação | `GET /health`, `GET /swagger/index.html` | 200 pelo VPC Link e ALB, com `X-Correlation-Id` |
| 01 · Autenticação do cliente por CPF | `POST /auth/cpf` | 200 com token de role `Cliente`; 400 para CPF inválido; 401 para CPF não cadastrado ou cliente inativo |
| 02 · Autenticação do funcionário | `POST /auth/login`, `POST /api/auth/login` | 200 com token administrativo; 401 com senha errada |
| 03 · Rotas protegidas | `GET /api/ordemservico/cliente/{clienteId}`, `GET /api/cliente` | 401 sem token, 403 com token inválido, 403 do cliente em rota administrativa (authorizer), 403 do cliente consultando outro cliente (API), 200 nos casos permitidos |
| 04 · Fluxo da ordem de serviço | abertura, diagnóstico, item, orçamento e PDF, aprovação, execução, entrega | Cada transição confere o status; a aprovação pode ser pelo link do e-mail ou pela oficina |

### 401 e 403: quem responde

- **401 `{"message":"Unauthorized"}`**: a requisição chegou sem `Authorization`; o gateway recusa
  antes de chamar o authorizer.
- **403 `{"message":"Forbidden"}`**: o authorizer negou (token inválido, expirado ou de cliente em
  rota administrativa). A API não é chamada, por isso a resposta não tem `X-Correlation-Id`.
- **403 `{"success":false,"errors":[...]}`**: a API negou pela posse do recurso. A resposta tem
  `X-Correlation-Id`, igual ao `requestId` do access log do gateway.

Detalhes em [ADR-002](../adr/ADR-002-autorizacao-em-duas-camadas.md).

### Aprovação pelo e-mail

Os clientes do seed têm e-mails fictícios. Para receber o link de verdade, preencha
`emailDemonstracao`: as duas primeiras requisições da pasta 04 trocam o e-mail do cliente antes
de abrir a OS. Quando o e-mail chegar, copie o token do link
(`.../api/aprovacaoordemservico/<token>/aprovar`) para a variável `tokenAprovacao` e execute
**Aprovar pelo link do e-mail**. Sem o token, a coleção aprova pela rota da oficina.

## Limite de taxa (429)

O stage limita as três rotas de autenticação (`POST /auth/cpf`, `POST /auth/login` e
`POST /api/auth/login`) a 5 req/s com rajada de 10. O Collection
Runner envia uma requisição por vez e, com a latência até `us-east-1`, dificilmente passa desse
limite. Para ver o 429, dispare em paralelo:

```bash
BASE_URL="https://<api-id>.execute-api.us-east-1.amazonaws.com"
seq 40 | xargs -P 20 -I{} curl -s -o /dev/null -w "%{http_code}\n" \
  -X POST "$BASE_URL/auth/cpf" -H "Content-Type: application/json" \
  -d '{"cpf":"935.411.347-80"}' | sort | uniq -c
```

O CPF é válido e não cadastrado, então as respostas esperadas são `401` e `429`: nenhum token é
emitido durante o teste.

## Validação da coleção

Os scripts foram executados contra um gateway simulado, que reproduz os códigos e os corpos de
resposta da Lambda, do authorizer e da API, nos dois caminhos da aprovação: pelo link do e-mail e
pela oficina. A execução contra o gateway real depende do ambiente aplicado na AWS.
