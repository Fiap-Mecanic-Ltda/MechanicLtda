# ADR-011 — Banco relacional gerenciado: SQL Server Express no Amazon RDS

- **Status:** aceito
- **Data:** 2026-09-13
- **Contexto detalhado:** [RFC-003](../rfc/RFC-003-escolha-do-banco-de-dados.md)
- **Modelo e relacionamentos:** [Banco de dados](../arquitetura/03-banco-de-dados.md)

## Contexto

O desafio exige banco gerenciado e modelagem documentada com consistência e performance. O
domínio é transacional e relacional, e a aplicação usa EF Core e ASP.NET Core Identity sobre SQL
Server desde a Fase 1.

## Decisão

- **Motor e serviço:** SQL Server no Amazon RDS, edição Express, `db.t3.micro`, 20 GiB `gp3`.
- **Rede:** sub-redes privadas sem rota para a internet; porta 1433 aberta só para os security
  groups dos nós do k3s e da Lambda de autenticação.
- **Proteção de dados:** criptografia em repouso no RDS e, na aplicação, CPF/CNPJ cifrado com AES
  e pesquisável apenas pelo índice cego HMAC ([ADR-003](ADR-003-indice-cego-cpf.md)).
- **Operação:** backup automático de 7 dias e atualização automática de versões menores.
- **Esquema:** evolui só por migrations do EF Core aplicadas no startup da API. A Lambda apenas
  lê e nunca altera o esquema.

## Consequências

- Integridade referencial, transações e precisão monetária garantidas pelo banco.
- Nenhuma migração de motor na Fase 3.
- Sem Multi-AZ e com banco limitado a 10 GB (edição Express): a saída é SQL Server Standard com
  Multi-AZ, ou PostgreSQL se o custo pesar mais, conforme a RFC-003.
- Sem proteção contra exclusão nem snapshot final, adequado ao projeto acadêmico e a ser revisto
  antes de haver dados reais de clientes.
