# ADR-003 — Índice cego (HMAC) para localizar o cliente pelo CPF

- **Status:** aceito
- **Data:** 2026-09-12
- **Contexto detalhado:** [RFC-001](../rfc/RFC-001-estrategia-api-gateway.md)

## Contexto

A Function serverless de autenticação precisa responder "existe um cliente com este CPF e ele
está ativo?". Só que `Cliente.CpfCnpj` é persistido cifrado por `CpfCnpjEncryptionConverter`,
com **IV aleatório**: o mesmo CPF gera textos cifrados diferentes a cada gravação. Consequências:

- `WHERE CpfCnpj = @cpf` nunca encontra nada;
- a alternativa seria ler todos os clientes e descriptografar um a um, o que obrigaria a
  Lambda a conhecer a chave de criptografia e a varrer a tabela inteira a cada login.

## Decisão

Adicionar a coluna **`CpfCnpjHash`** em `Clientes`, com `HMAC-SHA256` dos dígitos do documento
em hexadecimal minúsculo, e um **índice único filtrado** (`WHERE [CpfCnpjHash] IS NOT NULL`).

- A chave do HMAC é **separada** da chave de criptografia: `CPF_HASH_KEY` (variável de
  ambiente) ou `Encryption:CpfCnpjHashKey` (configuração).
- O documento é normalizado para dígitos antes do hash, então máscara não altera o resultado.
- A aplicação preenche o hash ao cadastrar e ao atualizar o cliente
  (`ClienteService`), e um backfill idempotente no start preenche as linhas antigas.
- O contrato com a Lambda é fixado por um vetor de teste em
  `DocumentoHashServiceTests.GerarHash_DeveHonrarOVetorDeContratoComALambda`: chave e CPF
  conhecidos precisam produzir um hash conhecido. Se o algoritmo divergir entre os dois
  repositórios, o teste quebra antes do deploy.

## Alternativas

- **Criptografia determinística** para o CPF: tornaria a coluna pesquisável, mas expõe padrão
  de repetição e exigiria reescrever os dados já cifrados.
- **Descriptografar tudo na Lambda**: espalha a chave de criptografia e faz varredura completa
  da tabela em cada autenticação.
- **Guardar o CPF em claro numa coluna indexada**: inaceitável para dado pessoal.

## Consequências

- A Lambda consulta `SELECT TOP 1 Id, Nome, Ativo FROM Clientes WHERE CpfCnpjHash = @hash`,
  com índice, e nunca precisa da chave de criptografia.
- O segredo `Encryption__CpfCnpjHashKey` passa a ser obrigatório nos deployments da API e do
  Web (repositório InfraKubernete) e na Lambda. Sem ele a aplicação sobe, mas cadastro e
  atualização de cliente falham de forma explícita — é uma falha alta e visível, em vez de
  gravar cliente sem índice e quebrar a autenticação silenciosamente.
- O índice é único: duas linhas com o mesmo CPF fazem o backfill falhar. O erro é registrado em
  log e a aplicação continua no ar; a correção do dado é manual.
- O CPF nunca aparece em log, nem no token.
