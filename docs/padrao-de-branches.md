# Padrão de branches

Todos os repositórios da organização seguem o mesmo fluxo:

```text
feature/* ou fix/*  →  desenvolvimento  →  homologacao  →  main
```

| Branch | Para que serve | O que roda |
|---|---|---|
| `desenvolvimento` | Integração das features (repositórios de aplicação) | Build e testes |
| `homologacao` | Versão candidata, conferida antes de ir para a `main` | Build, testes e `terraform plan` |
| `main` | O que está no ar | Build, testes, publicação de imagem e `terraform apply` |

Os repositórios só de infraestrutura (`*-InfraKubernete`, `*-InfraSGBD`, Lambda) podem não
ter `desenvolvimento`: as features entram direto em `homologacao`.

## Migração (novembro de 2026)

- `homolog` virou `homologacao` no InfraKubernete e nos três repositórios do
  OrcamentoPagamento. A `homolog` antiga só deve ser apagada depois que a mudança de
  `github_branches` no InfraKubernete (OIDC da role de CI/CD) for aplicada pela `main`:
  até lá, só a `homolog` assume a role na AWS.
- No MechanicLtda, `homologFiapFase4` foi promovida para `homologacao`. A `homolog` de
  julho tem três commits que nunca chegaram à `main` e foi mantida até o time decidir.
- No ordemservico-service e no execucaoproducao-service, `desenvolvimento` foi promovida para
  `homologacao`, que só tinha o commit inicial.
