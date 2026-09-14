# RFC-003 — Escolha do banco de dados gerenciado

| Campo | Valor |
|---|---|
| Status | Aceito |
| Data | 2026-09-13 |
| Autores | Time MechanicLtda (13SOAT) |
| Fase | Tech Challenge — Fase 3 |
| Decisão registrada em | [ADR-011](../adr/ADR-011-sql-server-gerenciado-rds.md) |
| Modelo e justificativa completa | [Banco de dados](../arquitetura/03-banco-de-dados.md) |

## 1. Contexto

O desafio exige um banco de dados gerenciado ("PostgreSQL, MySQL, SQL Server, etc.") e pede
para melhorar e documentar a modelagem, garantindo consistência e performance.

Estado atual:

- **SQL Server** desde a Fase 1, com 11 migrations do EF Core e as tabelas do ASP.NET Core
  Identity no mesmo banco.
- Em produção, **RDS SQL Server Express** (`db.t3.micro`, 20 GiB `gp3`, criptografia em repouso,
  backup de 7 dias, sem Multi-AZ), em sub-redes privadas.
- Nuvem mantida na AWS ([RFC-002](RFC-002-escolha-da-nuvem.md)).
- A Fase 3 acrescenta um consumidor novo: a Lambda de autenticação, que lê `Clientes` e as
  tabelas do Identity.

## 2. Requisitos do banco

1. **Integridade relacional**: OS pertence a cliente e veículo existentes; itens e orçamento não
   existem sem a OS; uma OS tem no máximo um orçamento.
2. **Transações entre tabelas**: incluir um item baixa o estoque, recalcula a OS e atualiza o
   orçamento.
3. **Precisão monetária** em itens, OS e orçamento.
4. **Compatibilidade** com EF Core, ASP.NET Core Identity e a Lambda .NET (SqlClient).
5. **Operação gerenciada**: backup, correções, criptografia e rede privada sem trabalho manual.
6. **Custo** compatível com um projeto acadêmico.

Os requisitos 1 a 3 eliminam bancos não relacionais: um banco de documentos ou chave-valor
empurraria integridade referencial e atomicidade entre agregados para dentro da aplicação.

## 3. Alternativas

| Critério | **RDS SQL Server Express (atual)** | RDS SQL Server Standard | RDS / Aurora PostgreSQL | DynamoDB |
|---|---|---|---|---|
| Integridade e transações | Sim | Sim | Sim | Transações limitadas; sem FK |
| EF Core e Identity | Provedor de primeira linha, já em uso | Idem | Provedor Npgsql, maduro | Sem suporte ao Identity |
| Migrations existentes | Aproveitadas | Aproveitadas | Recriar as 11 e revisar dialeto (ex.: filtro `[CpfCnpjHash] IS NOT NULL`, colunas `nvarchar`) | Reescrever a camada de dados |
| Alta disponibilidade | Sem Multi-AZ | Multi-AZ | Multi-AZ; Aurora com réplicas | Nativa |
| Limites | Banco de até 10 GB; memória e CPU limitadas pela edição | Sem os limites da Express | — | — |
| Custo relativo | O menor entre as edições de SQL Server | Várias vezes o da Express | Sem licença: menor que SQL Server na mesma classe | Por requisição; baixo neste volume |
| Esforço na Fase 3 | Nenhum | Troca de `engine` e `instance_class` | Migração de provedor, dados e testes | Reescrita |

## 4. Decisão

**Manter o SQL Server gerenciado no RDS, edição Express**, e investir a fase na qualidade do
modelo — índices e colunas que a autenticação e o monitoramento exigem — em vez de trocar o
motor.

Justificativa:

1. Atende a todos os requisitos funcionais: integridade, transações, precisão e o dialeto já
   usado pelas migrations.
2. É a opção de menor risco para o prazo: nenhuma migração de dados, de provedor ou de testes.
3. Os limites da Express (10 GB, sem Multi-AZ) estão distantes do volume do projeto e têm
   caminho de saída conhecido (Standard com Multi-AZ, ou PostgreSQL).
4. A Lambda de autenticação usa `Microsoft.Data.SqlClient` diretamente, sem ORM: o contrato com
   o banco é um `SELECT` por índice.

## 5. Ajustes no modelo feitos nesta fase

| Ajuste | Motivo |
|---|---|
| `Clientes.CpfCnpjHash` com índice único filtrado | Tornar o CPF cifrado pesquisável pela Lambda sem expor a chave de criptografia, e impedir dois clientes com o mesmo documento ([ADR-003](../adr/ADR-003-indice-cego-cpf.md)) |
| `OrdensServico.DataAlteracaoStatus` | Medir o tempo em cada etapa da OS para o painel de monitoramento |
| Preservação de `DataInicioExecucao` e `DataFimExecucao` na atualização da OS | O `Update` do EF Core apagava as datas, quebrando o tempo médio de execução |

Os ajustes recomendados e ainda não aplicados (transações explícitas, retry de falhas
transitórias, índices únicos de placa e e-mail, constraints de estoque) estão no
[documento do banco de dados](../arquitetura/03-banco-de-dados.md#42-ajustes-recomendados-não-aplicados).

## 6. Quando revisitar

- **Volume próximo de 10 GB** ou necessidade de réplica de leitura: SQL Server Standard.
- **Exigência de disponibilidade entre zonas**: Standard com Multi-AZ.
- **Custo virar restrição**: PostgreSQL no RDS, aceitando o esforço de migração descrito acima.

## Referências

- 13SOAT — Fase 3 — Tech Challenge (infraestrutura obrigatória e documentação da arquitetura).
- Repositório InfraSGBD — `infra/rds.tf`.
