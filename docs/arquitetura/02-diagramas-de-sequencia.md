# Diagramas de sequência

Fluxos de autenticação e de abertura de ordens de serviço, como implementados na Fase 3.
As rotas estão em minúsculas porque o roteamento do API Gateway diferencia maiúsculas.

## 1. Autenticação do cliente por CPF e consulta das próprias ordens de serviço

```mermaid
sequenceDiagram
    autonumber
    actor C as Cliente
    participant GW as API Gateway
    participant AUTH as Lambda de autenticação
    participant AZ as Lambda authorizer
    participant API as API .NET
    participant DB as RDS SQL Server

    C->>GW: POST /auth/cpf com o CPF
    Note over GW: Throttling de 5 req/s na rota
    GW->>AUTH: Invoca a função
    AUTH->>AUTH: Valida os dígitos verificadores

    alt CPF com dígito inválido
        AUTH-->>C: 400 CPF invalido, sem consultar o banco
    else CPF válido
        AUTH->>AUTH: Calcula HMAC-SHA256 do CPF
        AUTH->>DB: SELECT Id, Nome, Ativo WHERE CpfCnpjHash = hash
        DB-->>AUTH: Cliente ou nenhum resultado
        alt Cliente inexistente ou inativo
            AUTH-->>C: 401 com a mesma mensagem nos dois casos
        else Cliente ativo
            AUTH->>AUTH: Assina JWT com clienteId, role Cliente e validade de 30 min
            AUTH-->>C: 200 com token, expiração e dados do cliente
        end
    end

    C->>GW: GET /api/ordemservico/cliente/42 com Bearer token
    GW->>AZ: Authorization e routeKey
    AZ->>AZ: Valida assinatura, exp, aud e issuer
    AZ->>AZ: Role Cliente é permitida nesta rota?
    alt Token inválido ou rota administrativa
        AZ-->>GW: isAuthorized false
        GW-->>C: 403
    else Autorizado
        AZ-->>GW: isAuthorized true com clienteId e role
        Note over GW: Decisão em cache por 300 s para o mesmo token e rota
        GW->>API: VPC Link, ALB, NodePort com X-Correlation-Id
        API->>API: Valida o JWT e confere que clienteId do token é 42
        alt clienteId da rota diferente do token
            API-->>C: 403
        else Mesmo cliente
            API->>DB: Consulta as OS do cliente
            DB-->>API: Ordens de serviço
            API-->>C: 200 com as OS e seus status
        end
    end
```

## 2. Autenticação de funcionário por e-mail e senha

```mermaid
sequenceDiagram
    autonumber
    actor F as Funcionário
    participant GW as API Gateway
    participant API as API .NET
    participant DB as RDS SQL Server

    F->>GW: POST /api/auth/login com e-mail e senha
    Note over GW: Rota pública, throttling de 5 req/s
    GW->>API: VPC Link, ALB, NodePort
    API->>DB: Busca o usuário no Identity
    DB-->>API: Usuário, hash da senha e roles
    alt Usuário inexistente, inativo ou senha incorreta
        API-->>F: 400 Credenciais invalidas
    else Credenciais válidas
        API->>API: Emite JWT com role Funcionario e validade de 60 min
        API-->>F: 200 com token e expiração
    end
```

O mesmo login também está disponível pela Lambda em `POST /auth/login`, que lê as tabelas do
Identity e emite um token idêntico ao da API.

## 3. Abertura da ordem de serviço até a aprovação do orçamento

```mermaid
sequenceDiagram
    autonumber
    actor F as Funcionário
    actor C as Cliente
    participant GW as API Gateway
    participant AZ as Lambda authorizer
    participant API as API .NET
    participant DB as RDS SQL Server
    participant NR as New Relic
    participant MAIL as SMTP

    F->>GW: POST /api/ordemservico com cliente, veículo e problema
    GW->>AZ: Valida token e role
    AZ-->>GW: Autorizado, role Funcionario
    GW->>API: Encaminha com X-Correlation-Id
    API->>DB: Confere se o veículo pertence ao cliente
    API->>DB: INSERT da OS com status Recebida e DataAlteracaoStatus
    API->>NR: Evento OrdemServicoEvento tipo Criada
    API->>MAIL: E-mail de status Recebida
    Note over API,MAIL: Falha no SMTP gera o evento FalhaIntegracao e não desfaz a OS
    API-->>F: 200 com a OS criada

    F->>GW: PATCH /api/ordemservico/7/iniciar-diagnostico
    GW->>API: Autorizado pelo authorizer
    API->>DB: Status Em Diagnóstico
    API->>NR: MudancaStatus com minutos em Recebida
    API-->>F: 200

    F->>GW: POST /api/ordem-servico/7/itens com peças, insumos e serviços
    GW->>API: Autorizado pelo authorizer
    API->>DB: Baixa o estoque, grava o item, recalcula a OS e o orçamento
    API-->>F: 200

    F->>GW: PATCH /api/ordemservico/7/aguardar-aprovacao
    GW->>API: Autorizado pelo authorizer
    API->>DB: Status Aguardando Aprovação e novo token de aprovação
    API->>NR: MudancaStatus com minutos em Diagnóstico
    API->>MAIL: E-mail com links de aprovar e recusar
    MAIL-->>C: Links para o gateway
    API-->>F: 200

    C->>GW: GET /api/aprovacaoordemservico/token/aprovar
    Note over GW: Rota pública, o token do link é a credencial
    GW->>API: VPC Link, ALB, NodePort
    API->>DB: Valida token não usado e não expirado
    API->>DB: Status Em Execução e token marcado como utilizado
    API->>NR: MudancaStatus com minutos em Aguardando Aprovação
    API->>MAIL: E-mail de status Em Execução
    API-->>C: 200 Ordem de Servico aprovada
```

Depois da aprovação, o funcionário finaliza (`PATCH .../finalizar`) e entrega
(`PATCH .../entregar`) a OS. Cada transição registra o tempo no status anterior, o que alimenta o
painel "Tempo médio por status: Diagnóstico, Execução e Finalização".
