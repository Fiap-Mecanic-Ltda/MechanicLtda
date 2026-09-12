# ADR-004 — Segredos da Lambda por variável de ambiente, sem NAT nem VPC endpoint

- **Status:** aceito
- **Data:** 2026-09-12
- **Contexto detalhado:** [RFC-001](../rfc/RFC-001-estrategia-api-gateway.md)

## Contexto

A Lambda `auth-cpf` precisa alcançar o RDS, que é privado, então ela roda **dentro da VPC**.
Uma Lambda em VPC não tem saída para a internet a menos que exista NAT Gateway — e a VPC do
projeto não tem NAT (decisão de custo tomada na fase anterior). Sem saída, a função não
alcança o SSM Parameter Store nem o Secrets Manager para ler os segredos em tempo de execução.

As opções são: criar NAT Gateway (cerca de US$ 32 por mês), criar VPC endpoints de interface
para SSM e KMS (cerca de US$ 7 por mês por AZ) ou injetar os segredos como variáveis de
ambiente da função no momento do deploy.

## Decisão

O Terraform da Lambda lê os parâmetros do SSM **no momento do `apply`** e os injeta como
variáveis de ambiente da função (`JWT_SECRET_KEY`, `CPF_HASH_KEY`, `DB_CONNECTION_STRING`).
As variáveis de ambiente da Lambda são cifradas em repouso com chave gerenciada pela AWS.

Complementos da decisão:

- A `jwt-authorizer` **não** entra na VPC: ela só valida assinatura e claims, não faz I/O.
  Fica fora, com cold start menor e sem ENI.
- A conexão com o banco usa um login **somente leitura** (`SELECT` em `Clientes`), e não o
  usuário master do RDS.
- Os valores ficam no state do Terraform, que já é remoto, cifrado no S3 e de acesso restrito —
  o mesmo tratamento que a connection string do banco já recebe hoje.

## Consequências

- Sem custo adicional de NAT ou de endpoints.
- **Rotação de segredo exige redeploy da função**, e não apenas atualização do parâmetro no
  SSM. Isso precisa estar no procedimento de rotação da chave JWT.
- Se a Lambda passar a precisar de qualquer serviço externo (envio de e-mail, chamada HTTP a
  terceiros), a decisão precisa ser revisitada: aí entram VPC endpoint ou NAT.
