# RFC-004 — Estratégia de autenticação

| Campo | Valor |
|---|---|
| Status | Aceito |
| Data | 2026-09-13 |
| Autores | Time MechanicLtda (13SOAT) |
| Fase | Tech Challenge — Fase 3 |
| Relacionadas | [RFC-001](RFC-001-estrategia-api-gateway.md), [ADR-002](../adr/ADR-002-autorizacao-em-duas-camadas.md), [ADR-003](../adr/ADR-003-indice-cego-cpf.md), [ADR-004](../adr/ADR-004-segredos-da-lambda.md) |

## 1. Contexto

O desafio pede para "proteger rotas sensíveis da aplicação com autenticação via CPF", com uma
Function serverless que valide o CPF, consulte a existência e o status do cliente e devolva um
JWT válido para consumir as APIs protegidas.

O sistema tem dois públicos, que já eram tratados de formas diferentes:

| Público | Cadastro | Autenticação antes da Fase 3 | O que acessa |
|---|---|---|---|
| Administrador e funcionário | `AspNetUsers` (ASP.NET Core Identity) | E-mail e senha, JWT emitido pela API | Toda a operação da oficina |
| Cliente | `Clientes` (sem vínculo com `AspNetUsers`) | Nenhuma (só links de aprovação por e-mail) | O andamento das próprias ordens de serviço |

## 2. Alternativas

| Critério | **A. Lambda própria emite JWT + Lambda authorizer** | B. Amazon Cognito com fluxo customizado | C. Lambda emite JWT RS256 + JWT authorizer nativo | D. Plugin JWT do Kong |
|---|---|---|---|---|
| Atende "Function valida CPF, consulta status e devolve JWT" | Sim, literalmente | Parcial: a Lambda vira gatilho do Cognito, que emite o token | Sim | Não: o Kong valida, não emite |
| Onde fica o cadastro | `Clientes` no RDS (fonte única) | Pool do Cognito, sincronizado com `Clientes` | `Clientes` no RDS | `Clientes` + consumers do Kong |
| Mudança na API | Aceitar um segundo issuer | Validar tokens do Cognito (JWKS) e mapear claims | Validar RS256 via JWKS | Aceitar tokens do Kong |
| Filtro de role por rota no gateway | No authorizer | Escopos do Cognito | Escopos no JWT authorizer | Plugin ACL |
| Segredo compartilhado | Chave HS256 na Lambda e na API | Não | Não (chave privada só na Lambda) | Depende |
| Complexidade | Baixa | Alta: sincronização de cadastro e desafio customizado | Média: par de chaves, rotação e publicação do JWKS | Depende da escolha do Kong, rejeitada na RFC-001 |

## 3. Decisão

**Alternativa A**: a Lambda de autenticação expõe `POST /auth/cpf`, valida os dígitos
verificadores, localiza o cliente pelo índice cego do CPF, confere se está ativo e emite um JWT
HS256 com a mesma chave e audience da API e issuer próprio (`MechanicLtda.Auth.Cpf`). Um Lambda
authorizer valida o token nas rotas protegidas do gateway, e a API continua validando o token,
a role e a posse do recurso.

O login de administradores e funcionários continua por e-mail e senha (Identity), agora também
disponível pela Lambda em `POST /auth/login`.

### Contrato do token de cliente

| Claim | Valor |
|---|---|
| `iss` | `MechanicLtda.Auth.Cpf` |
| `aud` | `MechanicLtda.Clients` |
| `sub`, `clienteId` | Id do cliente |
| `role`, `tipo` | `Cliente` |
| `exp` | 30 minutos |

O CPF não vai no token nem nos logs.

## 4. Análise de segurança

**CPF não é segredo.** Ele circula em notas fiscais, cadastros e documentos. Autenticar só com o
CPF é, na prática, identificação — qualquer pessoa que conheça o CPF de um cliente obtém um
token dele. O desafio pede exatamente esse fluxo, então a decisão foi limitar o que esse token
permite e dificultar o abuso:

| Risco | Mitigação implementada |
|---|---|
| Acesso aos dados de um cliente com o CPF dele | O token de cliente só vale na rota de consulta das próprias OS (filtro do authorizer), a API confere que o `clienteId` da rota é o do token e o token expira em 30 minutos |
| Descobrir quem é cliente testando CPFs | Resposta idêntica (`401`) para CPF inexistente e cliente inativo; CPF com dígito inválido responde `400` sem consultar o banco |
| Força bruta | Throttling de 5 requisições por segundo nas rotas de autenticação e alarme de pico de `4xx` em `/auth/cpf` |
| Token de cliente usado em rota administrativa | Negado pelo authorizer (`403`) e pelas roles da API |
| Vazamento da chave HS256 permite emitir tokens | Chave só no SSM (`SecureString`) e nas variáveis de ambiente cifradas da Lambda e da API; rotação por novo apply e redeploy |

## 5. Trade-offs e evolução

| Aceito agora | Evolução recomendada |
|---|---|
| Autenticação de cliente com um único fator conhecido publicamente | Segundo fator: código de uso único enviado ao e-mail cadastrado do cliente, validado na mesma Lambda antes de emitir o token |
| Chave simétrica compartilhada entre Lambda e API | RS256 com JWKS publicado (alternativa C): só a Lambda assina, a API e o gateway apenas verificam, e o authorizer em Lambda pode ser trocado pelo JWT authorizer nativo do HTTP API |
| Cache de 300 s do authorizer | Um token revogado continua aceito no gateway por até 5 minutos; aceitável com a expiração de 30 minutos |

## Referências

- 13SOAT — Fase 3 — Tech Challenge (autenticação e API Gateway).
- POSTECH Software Architecture, Fase 3 — API Gateway, aulas 3 (políticas de autenticação) e 6
  (consumers e autenticação no Kong).
- Repositório Lambda — `src/MechanicLtda.Auth.Lambda` e `infra/cluster.tf`.
