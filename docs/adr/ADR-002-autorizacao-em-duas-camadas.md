# ADR-002 — Autorização em duas camadas: gateway autentica, API valida a posse

- **Status:** aceito
- **Data:** 2026-09-12
- **Contexto detalhado:** [RFC-001](../rfc/RFC-001-estrategia-api-gateway.md)

## Contexto

Com o gateway na frente, é tentador concentrar toda a autorização nele. Porém:

- o cache do Lambda authorizer é indexado pelas *identity sources* (o header `Authorization` e
  o `routeKey`), e não pelos valores do path. Uma decisão que dependa do `{clienteId}` da URL
  seria reaproveitada para outro `clienteId` enquanto o cache estivesse válido;
- a API continua acessível de dentro da VPC (outros pods, jobs, testes), então ela não pode
  delegar a decisão final a um componente de borda.

## Decisão

A autorização fica dividida:

| Camada | Responsabilidade |
|---|---|
| **API Gateway (Lambda authorizer)** | Autenticação: assinatura, `exp`, `iss` (dois emissores aceitos) e `aud`. Mais um filtro grosso de role por rota: token com role `Cliente` só passa nas rotas do cliente. Cache de 300 s. |
| **API .NET** | Autorização fina: `[Authorize(Roles = ...)]` como hoje, mais a **posse do recurso** — o `clienteId` do path precisa ser o do claim `clienteId` do token. |

A verificação de posse vive em `BaseController.PodeAcessarCliente`, para ser reutilizada nas
próximas rotas de cliente (veículos, orçamento em PDF).

## Consequências

- Nenhuma requisição sem token válido chega ao backend, e nenhuma decisão sensível ao path é
  cacheada de forma incorreta.
- Um token com role `Cliente` emitido pelo login por e-mail e senha não tem o claim
  `clienteId` e, portanto, recebe `403` nas rotas por cliente. O caminho do cliente é o token
  emitido pela Lambda a partir do CPF. O teste de integração
  `GetOrdemServicoPorCliente_ComTokenDeClienteSemVinculo_DeveRetornar403` fixa essa regra.
- Se no futuro o time quiser que o usuário Identity de tipo `Cliente` também acesse essas
  rotas, o caminho é vincular `Usuario` a `Cliente` e emitir o claim `clienteId` no login —
  sem mexer na regra de posse.
