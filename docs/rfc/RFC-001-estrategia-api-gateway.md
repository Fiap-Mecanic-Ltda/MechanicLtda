# RFC-001 — Estratégia de API Gateway e autenticação por CPF

| Campo | Valor |
|---|---|
| Status | Aceito — revisado após a implementação (ver seção 9) |
| Data | 2026-09-12 (revisão em 2026-09-13) |
| Autores | Time MechanicLtda (13SOAT) |
| Fase | Tech Challenge — Fase 3 |
| Repositórios afetados | MechanicLtda (aplicação), InfraKubernete, Lambda |

## 1. Contexto

A Fase 3 exige um API Gateway na frente da aplicação, proteção das rotas sensíveis com
autenticação via CPF e uma Function serverless que valide o CPF, consulte a existência e o
status do cliente e devolva um JWT válido.

O estado atual da plataforma:

- API e Web expostos diretamente pelo IP público da EC2 que roda o k3s, em HTTP, nas portas
  `8080` e `8090`, com o security group liberado para `0.0.0.0/0`.
- Autenticação feita pela própria API: JWT HS256 emitido em `POST /api/auth/login`
  (e-mail e senha, ASP.NET Identity), com as roles `Administrador`, `Funcionario` e `Cliente`.
- RDS SQL Server privado, acessível apenas pelos nós do cluster.
- Sem load balancer, sem HTTPS e sem ponto único de entrada.

## 2. Decisão proposta

Adotar o **AWS API Gateway (HTTP API)** como ponto único de entrada, integrado por
**VPC Link** a um **ALB interno** que distribui para o NodePort dos nós k3s, com:

- uma Lambda **`auth-cpf`** que recebe o CPF, valida os dígitos verificadores, consulta o
  cliente no RDS e emite o JWT;
- uma Lambda **`jwt-authorizer`** (Lambda authorizer, payload 2.0) que valida o token nas
  rotas protegidas, antes de a requisição chegar ao backend;
- a porta `8080` deixando de aceitar tráfego da internet, passando a aceitar somente o ALB.

## 3. Alternativas avaliadas

| Critério | HTTP API (proposto) | REST API | Kong OSS no k3s | Azure APIM |
|---|---|---|---|---|
| Operação | Gerenciado, serverless | Gerenciado | Pods nos nós `t3.small`, disputando CPU e memória com API e Web | Gerenciado, em outra nuvem |
| Integração com Lambda | Nativa (proxy e authorizer) | Nativa | Plugin `aws-lambda` | Azure Functions |
| Backend privado | VPC Link para ALB, NLB ou Cloud Map | VPC Link apenas para NLB | Nativo (Service ClusterIP) | — |
| Limite de taxa | Throttling por rota e por stage | Usage plans e API keys por consumidor | `rate-limiting` por consumer | Políticas |
| Cache de resposta | Não | Sim (cobrado) | `proxy-cache` | Sim |
| WAF | Não | Sim | Não | — |
| HTTPS | Incluso | Incluso | Exige certificado e ingress | Incluso |
| Custo | ~US$ 1,00 por milhão de requisições | ~US$ 3,50 por milhão | Custo dos nós | Tier Developer ~US$ 50/mês |
| Aula de referência | Serverless, aula 4 | Serverless, aula 4 | API Gateway, aulas 4 a 6 | API Gateway, aulas 2 e 3 |

## 4. Justificativa

1. **Aderência à nuvem já usada.** Toda a infraestrutura está na AWS, provisionada por
   Terraform. O HTTP API entra como mais um recurso do mesmo estado, sem novo provedor.
2. **O requisito de autenticação é serverless.** O HTTP API integra Lambda nativamente, tanto
   como backend de rota (`POST /auth/cpf`) quanto como authorizer, sem código de cola.
3. **Não consome o cluster.** Kong rodaria nos mesmos nós `t3.small` que hospedam API e Web,
   competindo por CPU e memória com a aplicação que ele deveria proteger.
4. **HTTPS e observabilidade inclusos.** O endpoint do gateway já é HTTPS, e as métricas de
   latência, contagem e erro por rota saem no CloudWatch — que é a fonte de dados exigida no
   requisito de monitoramento.
5. **Custo.** O HTTP API custa cerca de um terço do REST API por requisição e cai no free tier
   no volume do projeto.

## 5. Trade-offs aceitos

| Abrimos mão de | Por que é aceitável | Compensação |
|---|---|---|
| Cache de resposta (só no REST API) | Volume baixo e dados de OS mudam a cada interação | Se houver necessidade, cache na aplicação |
| WAF (só no REST API) | Superfície pequena e sem formulário público | Throttling agressivo na rota de login e alarme de 4XX |
| Cotas por consumidor (usage plans) | Não há múltiplos parceiros consumindo a API | Throttling por rota no stage |

## 6. Consequências

- O backend deixa de ser acessível pela internet: qualquer consumidor passa pelo gateway.
- A aplicação mantém a validação do JWT, das roles e da posse do recurso. O gateway autentica
  e filtra role por rota; a API decide o resto. Ver [ADR-002](../adr/ADR-002-autorizacao-em-duas-camadas.md).
- O CPF precisa de um índice determinístico para ser pesquisável. Ver
  [ADR-003](../adr/ADR-003-indice-cego-cpf.md).
- Surge um custo fixo de ALB (cerca de US$ 17 a 23 por mês), o maior item da conta desta feature.
- O token emitido pelo login por e-mail e senha com role `Cliente` deixa de acessar as rotas
  por `clienteId`, porque não carrega esse vínculo. O caminho do cliente passa a ser o token
  emitido pela Lambda a partir do CPF.

## 7. Escopo por repositório

| Repositório | O que entra |
|---|---|
| **Lambda** | `auth-cpf`, `jwt-authorizer`, Terraform das funções e CI/CD próprio |
| **InfraKubernete** | Sub-redes privadas, ALB interno, security groups, VPC Link, HTTP API, rotas, stage, access log e alarmes |
| **InfraSGBD** | Login somente leitura para a Lambda e regra de entrada na porta 1433 a partir do security group da função |
| **MechanicLtda** | Índice cego do CPF, segundo issuer aceito, verificação de posse, rotas em minúsculas, forwarded headers e correlação de log |

## 8. Pendências para decisão do time

| Pendência | Situação |
|---|---|
| Homologação: stack separada ou namespaces no mesmo cluster? | **Decidido:** sem ambiente de homologação na AWS — [ADR-009](../adr/ADR-009-sem-ambiente-de-homologacao.md) |
| O Web (Razor, porta `8090`) entra atrás do gateway nesta entrega? | Em aberto. O Web continua publicado direto |
| O cliente autenticado por CPF poderá abrir ordem de serviço ou apenas consultar? | **Decidido:** apenas consultar as próprias OS — [RFC-004](RFC-004-estrategia-de-autenticacao.md) |
| Domínio próprio com certificado ACM? | Em aberto. O projeto usa o endpoint padrão do API Gateway |

## 9. Revisão após a implementação

A decisão central — HTTP API, VPC Link, ALB interno, Lambda authorizer e backend privado — foi
implementada como proposta. Mudou a distribuição entre repositórios e alguns detalhes:

| Proposto nesta RFC | Implementado | Motivo |
|---|---|---|
| HTTP API, VPC Link e rotas no **InfraKubernete** | No repositório **Lambda** | O repositório Lambda já tinha um HTTP API publicando `POST /auth/login`. As rotas da aplicação, o VPC Link e o authorizer entraram no mesmo gateway, com uma única URL. O InfraKubernete provê o ALB interno, as sub-redes de aplicação e os outputs lidos por `terraform_remote_state` ([ADR-008](../adr/ADR-008-quatro-repositorios.md)) |
| Duas funções: `auth-cpf` e `jwt-authorizer` | Uma função de autenticação com duas rotas (`/auth/cpf` e `/auth/login`) e um authorizer, no mesmo pacote com handlers diferentes | A função existente autenticava e-mail e senha; o fluxo por CPF entrou como segunda rota, evitando um segundo cold start e um segundo conjunto de segredos |
| Resposta de `/auth/cpf` com `tokenType` e `expiresIn` | `{ token, expiracao, cliente }`, erro em `mensagem` | Mesmo formato do `/auth/login` que já existia |
| Login somente leitura no banco para a Lambda (InfraSGBD) | Não implementado; a Lambda usa a connection string existente | Registrado como ajuste recomendado no [documento do banco](../arquitetura/03-banco-de-dados.md) |
| Regra de entrada na porta 1433 no InfraSGBD | Criada no repositório da Lambda | Evita dependência circular: o banco não precisa saber que a Lambda existe |
| Alarmes no InfraKubernete | Alarmes do gateway e das Lambdas no repositório Lambda (CloudWatch); alertas da aplicação e do cluster no New Relic, no InfraKubernete | Cada alarme fica junto do recurso que monitora ([ADR-010](../adr/ADR-010-observabilidade-new-relic.md)) |

Dois problemas só apareceram na implementação e foram corrigidos na aplicação: os links de
aprovação por e-mail usavam `/api/AprovacaoOrdemServico`, que o roteamento do gateway, sensível a
maiúsculas, mandaria para a rota protegida; e o link "API" do Web apontava para a porta 8080, que
deixa de responder depois da virada.
