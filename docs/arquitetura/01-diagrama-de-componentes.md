# Diagrama de componentes

Visão de nuvem, APIs, banco e monitoramento do MechanicLtda na Fase 3. Os diagramas usam
Mermaid e são renderizados pelo próprio GitHub.

## 1. Nuvem, APIs e banco

```mermaid
flowchart LR
    CLIENTE["Cliente da oficina<br/>app, Postman ou Swagger"]
    EQUIPE["Funcionário e administrador<br/>navegador"]

    subgraph AWS["AWS us-east-1"]
        APIGW["API Gateway HTTP API<br/>HTTPS, throttling por rota"]
        AUTH["Lambda de autenticação<br/>POST /auth/cpf e /auth/login"]
        AUTHZ["Lambda authorizer<br/>token e role por rota"]
        SSM["SSM Parameter Store<br/>chave JWT, hash do CPF,<br/>connection string"]

        subgraph VPC["VPC 10.0.0.0/16"]
            subgraph SUBAPP["Sub-redes de aplicação - privadas, 2 AZs"]
                VPCLINK["VPC Link"]
                ALB["ALB interno<br/>health check /health"]
            end

            subgraph K3S["Cluster k3s - sub-rede pública<br/>EC2 server e ASG de workers 1 a 3"]
                API["API .NET 9<br/>HPA 1 a 5 pods"]
                WEB["Web Razor<br/>HPA 1 a 5 pods"]
            end

            subgraph SUBDB["Sub-redes de banco - privadas, 2 AZs"]
                RDS[("RDS SQL Server Express<br/>cifrado, backup 7 dias")]
            end
        end
    end

    SMTP["SMTP do Gmail"]
    CAIXA["E-mail do cliente"]

    CLIENTE -- "HTTPS" --> APIGW
    APIGW -- "/auth/cpf, /auth/login" --> AUTH
    APIGW -. "rotas protegidas" .-> AUTHZ
    APIGW -- "/api, /health, /swagger" --> VPCLINK
    VPCLINK -- "HTTP 80" --> ALB
    ALB -- "NodePort 8080" --> API
    EQUIPE -- "HTTP 8090" --> WEB
    AUTH -- "SQL 1433, leitura por CpfCnpjHash" --> RDS
    API -- "EF Core 1433" --> RDS
    WEB -- "EF Core 1433" --> RDS
    API -- "status e link de aprovação" --> SMTP
    SMTP --> CAIXA
    CAIXA -. "clique no link" .-> APIGW
    SSM -. "segredos no apply" .-> AUTH
    SSM -. "segredos no deploy" .-> K3S
```

Os nós do cluster ficam na sub-rede pública porque precisam de saída para a internet (ECR e SSM)
e a VPC não tem NAT Gateway. Depois da virada para o gateway (`expose_nodeport_publicly = false`
no InfraKubernete), a porta 8080 da API passa a aceitar tráfego só do security group do ALB, e o
único caminho de fora até a API é o gateway. Até lá, a 8080 continua pública para não
interromper o ambiente durante a validação.

## 2. Monitoramento

```mermaid
flowchart LR
    subgraph AWS["AWS"]
        APIGW["API Gateway"]
        LAMBDAS["Lambdas de autenticação<br/>e authorizer"]

        subgraph K3S["Cluster k3s"]
            API["API .NET 9<br/>agente APM"]
            WEB["Web Razor<br/>agente APM"]
            NRK8S["Integração Kubernetes<br/>nri-bundle"]
        end

        CW["CloudWatch<br/>access log, logs JSON,<br/>métricas por rota"]
        ALARMES["Alarmes<br/>5xx, latência p95, erros,<br/>throttling, 4xx em /auth/cpf"]
        SNS["SNS e e-mail"]
    end

    subgraph NR["New Relic"]
        APM["APM e logs in context<br/>latência, erros, traces"]
        EVENTOS["Eventos de negócio<br/>OrdemServicoEvento<br/>OrdemServicoFalha<br/>FalhaIntegracao"]
        INFRA["Kubernetes<br/>CPU, memória,<br/>reinícios, HPA"]
        SINT["Monitor sintético<br/>/health a cada 5 min"]
        PAINEL["Painel<br/>Ordens de serviço, APIs,<br/>Integrações, Kubernetes"]
        ALERTAS["Alertas<br/>latência, taxa de erro,<br/>falha de OS, e-mail,<br/>recursos, uptime"]
    end

    API -. "traces e logs" .-> APM
    WEB -. "traces e logs" .-> APM
    API -. "eventos" .-> EVENTOS
    WEB -. "eventos" .-> EVENTOS
    NRK8S -. "métricas" .-> INFRA
    SINT -- "HTTPS" --> APIGW
    APIGW -. "logs e métricas" .-> CW
    LAMBDAS -. "logs e métricas" .-> CW
    CW --> ALARMES
    ALARMES --> SNS
    APM --> PAINEL
    EVENTOS --> PAINEL
    INFRA --> PAINEL
    APM --> ALERTAS
    EVENTOS --> ALERTAS
    INFRA --> ALERTAS
    SINT --> ALERTAS
```

O API Gateway e as Lambdas só aparecem no New Relic com a integração AWS da conta; até lá, esse
trecho é monitorado pelos alarmes do CloudWatch ([ADR-010](../adr/ADR-010-observabilidade-new-relic.md)).

## Componentes

| Componente | Responsabilidade | Repositório |
|---|---|---|
| API Gateway HTTP API | Ponto único de entrada da API: HTTPS, roteamento, autenticação na borda, throttling e access log | Lambda |
| Lambda de autenticação | Valida o CPF, consulta existência e status do cliente e emite o JWT; também autentica funcionários por e-mail e senha | Lambda |
| Lambda authorizer | Autentica o token e filtra role por rota antes de a requisição chegar ao cluster | Lambda |
| VPC Link e ALB interno | Levam o tráfego do gateway até os nós do cluster sem expô-los à internet | Lambda (VPC Link) e InfraKubernete (ALB) |
| Cluster k3s | Executa API e Web, com HPA nos pods e ASG nos nós | InfraKubernete |
| API .NET 9 | Regras de negócio da oficina, validação de JWT, roles e posse do recurso | MechanicLtda |
| Web Razor | Interface administrativa, com autenticação por cookie | MechanicLtda |
| RDS SQL Server | Dados de clientes, veículos, OS, estoque, orçamentos e usuários | InfraSGBD |
| SSM Parameter Store | Segredos compartilhados entre aplicação, cluster e Lambda | InfraKubernete e InfraSGBD |
| New Relic | APM, logs, integração Kubernetes, eventos de negócio, painel, alertas e monitor sintético | MechanicLtda e InfraKubernete |
| CloudWatch e SNS | Logs, métricas e alarmes do gateway e das Lambdas | Lambda |

## 3. Entrega: repositórios, pipelines e contratos

```mermaid
flowchart LR
    subgraph GITHUB["GitHub - Fiap-Mecanic-Ltda"]
        R_APP["MechanicLtda<br/>aplicação"]
        R_K8S["InfraKubernete<br/>cluster, manifestos,<br/>observabilidade"]
        R_DB["InfraSGBD<br/>banco"]
        R_LAMBDA["Lambda<br/>funções e gateway"]
    end

    subgraph AWS["AWS"]
        ECR["ECR"]
        STATE["S3 - states do Terraform"]
        SSM["SSM Parameter Store"]
        CLUSTER["Cluster k3s"]
        RDS[("RDS")]
        GATEWAY["API Gateway e Lambdas"]
    end

    NR["New Relic"]

    R_APP -- "push na main: imagens com tag do commit" --> ECR
    R_APP -- "repository_dispatch imagem-publicada" --> R_K8S
    R_K8S -- "terraform apply" --> STATE
    R_K8S -- "deploy via SSM Run Command" --> CLUSTER
    ECR -- "pull das imagens" --> CLUSTER
    R_K8S -- "grava segredos" --> SSM
    R_K8S -- "terraform apply de alertas e painel" --> NR
    R_DB -- "terraform apply" --> RDS
    R_DB -- "connection string" --> SSM
    R_LAMBDA -- "terraform apply e smoke test" --> GATEWAY
    STATE -. "outputs de rede e ALB" .-> R_DB
    STATE -. "outputs de rede, ALB e RDS" .-> R_LAMBDA
    SSM -. "segredos lidos no apply" .-> R_LAMBDA
```

Os contratos entre os repositórios — ECR, `terraform_remote_state`, SSM e `repository_dispatch` —
e a ordem de subida estão na [ADR-008](../adr/ADR-008-quatro-repositorios.md). O que cada branch
dispara está na [ADR-009](../adr/ADR-009-sem-ambiente-de-homologacao.md).
