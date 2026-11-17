# MechanicLtda

Sistema de gestão para uma oficina mecânica (ordens de serviço, veículos, clientes, estoque e
orçamentos), desenvolvido em **.NET 9** seguindo princípios de DDD, Clean Architecture e
Clean Code.

## Documentação da arquitetura

A documentação arquitetural da Fase 3 está em [`docs/arquitetura`](docs/arquitetura/README.md).
Os diagramas são Mermaid e o GitHub os renderiza direto no navegador.

| Item | Documento |
|---|---|
| Diagrama de componentes — nuvem, APIs, banco e monitoramento | [01 — Diagrama de componentes](docs/arquitetura/01-diagrama-de-componentes.md) |
| Diagramas de sequência — autenticação e abertura de ordem de serviço | [02 — Diagramas de sequência](docs/arquitetura/02-diagramas-de-sequencia.md) |
| Justificativa do banco, ajustes no modelo relacional, diagrama ER e relacionamentos | [03 — Banco de dados](docs/arquitetura/03-banco-de-dados.md) |
| RFCs — nuvem, banco de dados, autenticação e API Gateway | [`docs/rfc`](docs/rfc) |
| ADRs — padrão de comunicação, HPA e outras nove decisões permanentes | [`docs/adr`](docs/adr) |

Também em `docs/`: a [coleção Postman](docs/postman/README.md) do fluxo pelo gateway, o
[plano do API Gateway](docs/api-gateway/plano-api-gateway.md) e a
[entrega da fase](docs/entrega/README.md) — roteiro do vídeo e gerador do PDF.

## Repositórios do projeto

O projeto é distribuído em **quatro repositórios**, cada um com a sua própria esteira de CI/CD:

| # | Repositório | Conteúdo | Pipeline |
|---|---|---|---|
| 1 | [Lambda](https://github.com/Fiap-Mecanic-Ltda/Lambda) | Function serverless (API Gateway + Lambda) que valida o usuário e emite o JWT | build, testes e deploy da função |
| 2 | [InfraKubernete](https://github.com/Fiap-Mecanic-Ltda/InfraKubernete) | Terraform da infra de Kubernetes (VPC, EC2/k3s, ASG, ECR, SSM, IAM/OIDC) + manifestos `k8s/` | `terraform plan/apply` e deploy no cluster |
| 3 | [InfraSGBD](https://github.com/Fiap-Mecanic-Ltda/InfraSGBD) | Terraform do RDS SQL Server (instância, subnet group, security group, connection string no SSM) | `terraform plan/apply` |
| 4 | **MechanicLtda** (este repo) | Aplicação .NET 9 (`src/`, `tests/`, Dockerfiles) | build, testes e push das imagens no ECR |

O contrato entre este repositório e os demais é o **ECR**: aqui as imagens são publicadas
(`mechanicltda-api` / `mechanicltda-web`, com as tags `latest` e o SHA do commit) e o deploy no
cluster é feito pelo workflow do repositório de infra de Kubernetes.

## Sobre esta fase (Tech Challenge — Fase 3)

A Fase 1 entregou a API de gestão da oficina e a Fase 2 a levou para a AWS: containerizada,
orquestrada em Kubernetes com HPA, provisionada por Terraform e publicada por uma esteira de
CI/CD. Esta fase fecha o ambiente atrás de um ponto único de entrada, tira a autenticação do
cliente de dentro do cluster e torna o sistema observável:

- **API Gateway como única porta de entrada**: o AWS API Gateway (HTTP API) recebe o tráfego em
  HTTPS e alcança o cluster por VPC Link e ALB interno, com throttling e access log por rota — os
  nós deixam de responder direto na internet. Ver
  [ADR-001](docs/adr/ADR-001-api-gateway-http-api.md).
- **Autenticação serverless por CPF**: uma Lambda valida o CPF, localiza o cliente pelo índice
  cego e emite o JWT; uma segunda Lambda (*authorizer*) valida o token e filtra a role antes de a
  requisição chegar ao cluster, e a API ainda confere a posse do recurso. Ver
  [RFC-004](docs/rfc/RFC-004-estrategia-de-autenticacao.md) e
  [ADR-002](docs/adr/ADR-002-autorizacao-em-duas-camadas.md).
- **Banco de dados gerenciado**: SQL Server no RDS, cifrado em repouso, em sub-redes privadas e
  com backup automático. Ver [RFC-003](docs/rfc/RFC-003-escolha-do-banco-de-dados.md).
- **Observabilidade ponta a ponta**: APM, logs, traces e eventos de negócio no New Relic, com
  painel, alertas e monitor sintético; alarmes do CloudWatch na borda serverless. Ver
  [ADR-010](docs/adr/ADR-010-observabilidade-new-relic.md).
- **Documentação da arquitetura**: diagramas de componentes e de sequência, modelo de dados,
  RFCs e ADRs — indexados no início deste README.

## Arquitetura

### Componentes da aplicação

O projeto segue o padrão de **Arquitetura em Camadas (Layered / Clean Architecture)**, com dois
hosts de apresentação (API REST e um front-end Web MVC) compartilhando as mesmas camadas de
domínio e aplicação:

```text
MechanicLtda/
├── src/
│   ├── MechanicLtda.API            # Host REST: Controllers, ViewModels, Swagger, JWT
│   ├── MechanicLtda.Web             # Host MVC: Views Razor, cookie, clientes HTTP dos microsserviços
│   ├── MechanicLtda.Bootstrap      # Composition root compartilhado por API e Web (DI, DB, migrations, seed)
│   ├── MechanicLtda.Application    # AppServices, DTOs e autenticação
│   ├── MechanicLtda.Domain         # Entidades, enums, interfaces e serviços de domínio
│   └── MechanicLtda.Infrastructure # Repositórios, DbContext, Fluent API, migrations e criptografia
├── tests/
│   ├── MechanicLtda.API.IntegrationTests
│   ├── MechanicLtda.Application.Tests
│   ├── MechanicLtda.Domain.Tests
│   └── MechanicLtda.Web.Tests
└── .github/workflows/               # Pipeline de CI/CD (GitHub Actions): build, testes e push no ECR
```

### Responsabilidades

| Projeto | Responsabilidade |
|---|---|
| `API` | Exposição dos endpoints HTTP, validações, Swagger, autenticação JWT e autorização por roles |
| `Web` | Interface administrativa via Razor Views, autenticação por cookie. OS, itens, estoque e serviços vêm do OrdemServico-service e do ExecucaoProducao-service por HTTP |
| `Bootstrap` | Composition root: injeção de dependências, conexão com banco, migrations e seed — compartilhado por `API` e `Web` para não duplicar esse código entre os dois hosts |
| `Application` | Orquestração dos casos de uso, DTOs, AutoMapper, login e troca de senha |
| `Domain` | Regras de negócio, entidades, enums, contratos de repositórios e serviços de domínio |
| `Infrastructure` | EF Core, SQL Server, Identity, migrations, repositórios e criptografia de CPF/CNPJ |

### Infraestrutura provisionada

| Componente | Tecnologia | Repositório / arquivo |
|---|---|---|
| Orquestração de containers | Kubernetes (**k3s**, rodando numa EC2) | InfraKubernete → `k8s/` |
| Banco de dados | SQL Server, gerenciado (**RDS**) em produção / container em dev local | InfraSGBD → `infra/rds.tf` |
| Registro de imagens | **ECR** (um repositório para API, outro para Web) | InfraKubernete → `infra/ecr.tf` |
| Segredos da aplicação | **SSM Parameter Store** (`SecureString`) | InfraKubernete → `infra/ssm.tf` |
| Autenticação da pipeline | **OIDC** do GitHub Actions → IAM Role (sem access keys estáticas) | InfraKubernete → `infra/github_oidc.tf` |
| Rede | VPC dedicada, subnet pública (EC2) + subnets privadas (RDS) | InfraKubernete → `infra/network.tf` |
| Autenticação serverless | **API Gateway + Lambda** emitindo o JWT | Lambda → `src/` + `infra/` |

O detalhamento completo de cada recurso e as decisões de design estão nos READMEs dos
repositórios de infraestrutura.

### Fluxo de deploy (CI/CD)

```text
git push (main)  ── repositório da aplicação (este)
   │
   ▼
GitHub Actions (.github/workflows/main.yml)
   ├─ 1. Build da aplicação (dotnet build)
   ├─ 2. Testes automatizados (dotnet test)
   └─ 3. Build e push das imagens Docker (API + Web) → ECR
              │
              ▼
        workflow "Deploy no Kubernetes" ── repositório InfraKubernete
              ├─ resolve a última imagem publicada no ECR
              ├─ renderiza os manifestos (kustomize, overlay prod)
              ├─ monta o Secret do Kubernetes a partir do SSM Parameter Store
              └─ aplica no k3s via SSM Run Command e aguarda o rollout
```

A API do Kubernetes (porta 6443) nunca é exposta publicamente — a pipeline de deploy autentica na
AWS via OIDC e usa `aws ssm send-command` para rodar `kubectl apply` diretamente na instância, o
mesmo mecanismo usado para qualquer acesso administrativo ao servidor (sem SSH).

## Tecnologias

- [.NET 9](https://dotnet.microsoft.com/)
- ASP.NET Core Web API + ASP.NET Core MVC (host Web administrativo)
- Entity Framework Core com SQL Server
- ASP.NET Core Identity (`IdentityDbContext<Usuario>`)
- JWT Bearer (API) / autenticação por cookie (Web)
- Autorização por roles: `Administrador`, `Funcionario` e `Cliente`
- AutoMapper
- Swagger / OpenAPI com suporte a Bearer Token
- xUnit, Moq, `WebApplicationFactory` e EF Core InMemory nos testes
- **Docker** e **Docker Compose** (desenvolvimento local)
- **Kubernetes** (k3s) com Kustomize (`base` + `overlays`) e Horizontal Pod Autoscaler
- **Terraform** (Infraestrutura como Código) — EC2, RDS, ECR, SSM Parameter Store, IAM/OIDC
  (nos repositórios de infraestrutura)
- **AWS Lambda + API Gateway** para a autenticação serverless (repositório próprio)
- **GitHub Actions** (CI/CD): aqui, build, testes e push das imagens no ECR; o deploy no cluster
  roda na esteira do repositório de infra

### Justificativa do SQL Server

O SQL Server foi escolhido por oferecer um banco relacional robusto para o domínio da oficina, em que ordens de serviço, itens, estoque, clientes, veículos, orçamentos e usuários exigem integridade referencial, transações consistentes e consultas estruturadas. A escolha também se encaixa bem com o Entity Framework Core, ASP.NET Core Identity e migrations, reduzindo atrito na evolução do schema e facilitando execução local ou via Docker Compose com SQL Server 2022.

A justificativa formal — por que relacional, por que SQL Server, por que gerenciado e os
trade-offs aceitos — está em [03 — Banco de dados](docs/arquitetura/03-banco-de-dados.md),
junto com o diagrama ER e os ajustes feitos no modelo relacional.

## Modelo de Domínio

| Entidade | Descrição |
|---|---|
| `Usuario` | Usuário do sistema, baseado em Identity, com tipo e estado ativo/inativo |
| `Cliente` | Cliente da mecânica, com CPF/CNPJ validado e criptografado no banco |
| `Veiculo` | Veículo vinculado a um cliente, com validação de placa antiga ou Mercosul |

Ordem de serviço, itens, estoque, serviços da oficina e orçamento estão nos microsserviços da
Fase 4 (seção [Endpoints](#endpoints)). As tabelas antigas continuam no banco como histórico,
mas este código não as lê mais.

### Enums principais

| Enum | Valores |
|---|---|
| `TipoUsuario` | `1 = Administrador`, `2 = Funcionario`, `3 = Cliente` |

## Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server local/remoto ou Docker
- Docker + Docker Compose, caso prefira executar a stack em containers

## Configuração

### Localmente

Configure a string de conexão, os metadados do JWT e a chave de criptografia em `src/MechanicLtda.API/appsettings.json`, User Secrets ou variáveis de ambiente:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=MechanicLtda;..."
  },
  "JwtSettings": {
    "Issuer": "MechanicLtda.API",
    "IssuerCpf": "MechanicLtda.Auth.Cpf",
    "Audience": "MechanicLtda.Clients",
    "ExpiracaoMinutos": 60
  },
  "Encryption": {
    "CpfCnpjKey": "sua-chave-de-criptografia-com-no-minimo-32-caracteres",
    "CpfCnpjHashKey": "sua-chave-de-hash-com-no-minimo-32-caracteres"
  }
}
```

| Chave | Uso |
|---|---|
| `JwtSettings:Issuer` | Emissor dos tokens gerados pelo login por e-mail e senha |
| `JwtSettings:IssuerCpf` | Emissor aceito para os tokens da Function serverless de autenticação por CPF |
| `Encryption:CpfCnpjKey` | Criptografia do CPF/CNPJ no banco |
| `Encryption:CpfCnpjHashKey` | Chave do índice cego (HMAC) que permite localizar o cliente pelo CPF — precisa ser **a mesma** configurada na Lambda, senão a autenticação por CPF não encontra o cliente. Também pode vir da variável de ambiente `CPF_HASH_KEY` |

A chave usada para assinar e validar tokens JWT é lida da variável de ambiente `JWT_SECRET_KEY`:

```powershell
$env:JWT_SECRET_KEY = "sua-chave-jwt-secreta-com-no-minimo-32-caracteres"
```

> A aplicação aplica migrations pendentes automaticamente ao iniciar e também executa o seed inicial quando o banco está vazio.

### Com Docker Compose

O `docker-compose.yml` usa variáveis de ambiente para injetar segredos nos serviços.

Crie um arquivo `.env` na raiz do projeto:

```env
SA_PASSWORD=SuaSenhaForte@123
JWT_SECRET_KEY=sua-chave-jwt-secreta-com-no-minimo-32-caracteres
ENCRYPTION_KEY=sua-chave-de-criptografia-com-no-minimo-32-caracteres
CPF_HASH_KEY=sua-chave-de-hash-com-no-minimo-32-caracteres
```

| Variável | Uso |
|---|---|
| `SA_PASSWORD` | Senha do usuário `sa` do SQL Server |
| `JWT_SECRET_KEY` | Chave secreta para assinatura e validação dos tokens JWT |
| `ENCRYPTION_KEY` | Chave de criptografia dos campos CPF/CNPJ |
| `CPF_HASH_KEY` | Chave do índice cego (HMAC) do CPF/CNPJ, compartilhada com a Lambda de autenticação |

> Nunca commite o arquivo `.env`.

## Como executar

### Localmente

```bash
dotnet restore
dotnet build
dotnet run --project src/MechanicLtda.API
```

A API estará disponível em:

- HTTP: `http://localhost:5062`
- HTTPS: `https://localhost:7013`
- Swagger: `http://localhost:5062/swagger` em ambiente de desenvolvimento

Caso queira aplicar migrations manualmente:

```bash
dotnet ef database update --project src/MechanicLtda.Infrastructure --startup-project src/MechanicLtda.API
```

### Com Docker Compose

```bash
docker compose up --build
```

Isso irá:

1. Subir o **SQL Server 2022** (`mechanicltda-sqlserver`) na porta `1433`.
2. Aguardar o SQL Server ficar saudável.
3. Construir e subir a **API** (`mechanicltda-api`) na porta `8080`.
4. Aplicar migrations e executar o seed inicial automaticamente.

A API estará disponível em `http://localhost:8080` (Swagger em `http://localhost:8080/swagger`) e o Web em `http://localhost:8090`.

Para parar os serviços:

```bash
docker compose down
```

Para remover também o volume de dados do SQL Server:

```bash
docker compose down -v
```

## Infraestrutura e deploy (outros repositórios)

O Terraform e os manifestos do Kubernetes **não ficam mais aqui**. Este repositório só publica as
imagens no ECR; provisionamento e deploy vivem nos repositórios de infraestrutura:

| O que | Onde |
|---|---|
| VPC, EC2 com k3s, ASG de workers, ECR, SSM, IAM/OIDC | [InfraKubernete](https://github.com/Fiap-Mecanic-Ltda/InfraKubernete) → `infra/` |
| Manifestos Kubernetes (Kustomize `base` + `overlays`) e o workflow de deploy | [InfraKubernete](https://github.com/Fiap-Mecanic-Ltda/InfraKubernete) → `k8s/` |
| RDS SQL Server (instância, subnet group, SG, connection string no SSM) | [InfraSGBD](https://github.com/Fiap-Mecanic-Ltda/InfraSGBD) → `infra/` |
| Lambda de autenticação + API Gateway | [Lambda](https://github.com/Fiap-Mecanic-Ltda/Lambda) → `src/` + `infra/` |

Ordem de subida do ambiente do zero:

1. `terraform apply` no **InfraKubernete** (rede, EC2/k3s, ECR, IAM/OIDC).
2. `terraform apply` no **InfraSGBD** (RDS + connection string no SSM).
3. Push na `main` **aqui** — publica as imagens no ECR.
4. Workflow *Deploy no Kubernetes*, no **InfraKubernete** — sobe a aplicação no cluster.

Para rodar os manifestos localmente (kind/minikube), clone o InfraKubernete e siga o
`k8s/README.md` de lá:

```bash
kind load docker-image mechanicltda-api:latest mechanicltda-web:latest
kubectl apply -k k8s/overlays/local      # dentro do repositório InfraKubernete
kubectl get pods,svc,hpa -n mechanicltda
```

Para desenvolvimento local sem Kubernetes, o `docker-compose.yml` deste repositório continua
sendo o caminho mais curto (ver a seção anterior).

## Autenticação e Autorização

A API utiliza **JWT Bearer Token**.

1. Realize login via `POST /api/auth/login` informando `email` e `senha`.
2. Copie o token retornado.
3. No Swagger, clique em **Authorize** e informe `Bearer {seu_token}`.

Todos os endpoints exigem autenticação, exceto `POST /api/auth/login`.

### Perfis de acesso

| Perfil | Acesso principal |
|---|---|
| `Administrador` | Gestão completa, incluindo usuários |
| `Funcionario` | Fluxos operacionais de clientes, veículos, estoque, ordens, itens e orçamentos |
| `Cliente` | Consulta de progresso das próprias ordens de serviço, com o token emitido pela autenticação por CPF (que carrega o claim `clienteId`) |

## Endpoints

### Auth

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `POST` | `/api/auth/login` | Público | Realiza login e retorna o token JWT |
| `PUT` | `/api/auth/alterar-senha` | Autenticado | Altera a senha do usuário |

### Usuários

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/usuario` | Administrador | Lista todos os usuários |
| `POST` | `/api/usuario` | Administrador | Cria um novo usuário |
| `PUT` | `/api/usuario/{id}` | Administrador | Atualiza um usuário |
| `DELETE` | `/api/usuario/{id}` | Administrador | Remove um usuário |

### Clientes

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/cliente` | Administrador, Funcionario | Lista todos os clientes |
| `GET` | `/api/cliente/{id}` | Administrador, Funcionario | Retorna um cliente pelo Id |
| `POST` | `/api/cliente` | Administrador, Funcionario | Cria um novo cliente |
| `PUT` | `/api/cliente/{id}` | Administrador, Funcionario | Atualiza um cliente |
| `DELETE` | `/api/cliente/{id}` | Administrador, Funcionario | Remove um cliente |

### Veículos

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/veiculo` | Administrador, Funcionario | Lista todos os veículos |
| `GET` | `/api/veiculo/{id}` | Administrador, Funcionario | Retorna um veículo pelo Id |
| `GET` | `/api/veiculo/cliente/{clienteId}` | Administrador, Funcionario | Lista veículos de um cliente |
| `POST` | `/api/veiculo` | Administrador, Funcionario | Cria um novo veículo |
| `PUT` | `/api/veiculo/{id}` | Administrador, Funcionario | Atualiza um veículo |
| `DELETE` | `/api/veiculo/{id}` | Administrador, Funcionario | Remove um veículo |

### Ordens de serviço, itens, estoque, serviços e orçamento

Saíram deste repositório na Fase 4. Ordem de serviço, itens e histórico de status estão no
[OrdemServico-service](https://github.com/Fiap-Mecanic-Ltda/OrdemServico-service); estoque,
catálogo de serviços e a fila da oficina, no
[ExecucaoProducao-service](https://github.com/Fiap-Mecanic-Ltda/ExecucaoProducao-service);
orçamento e pagamento, no
[OrcamentoPagamento-service](https://github.com/Fiap-Mecanic-Ltda/OrcamentoPagamento-service).
Cada serviço documenta as próprias rotas no Swagger. O Web continua com as mesmas telas, agora
chamando os serviços por HTTP (`src/MechanicLtda.Web/Integracoes`). A aprovação do orçamento
por link de e-mail deixou de existir: o cliente aprova pagando no Billing.

## Validações e Segurança

- CPF/CNPJ de clientes é validado na API e criptografado no banco via `CpfCnpjEncryptionConverter`.
- Além do valor cifrado, o cliente guarda o **índice cego** `CpfCnpjHash` (HMAC-SHA256 dos dígitos). É o que a Function serverless de autenticação usa para localizar o cliente pelo CPF sem descriptografar a coluna — que, por usar IV aleatório, não é pesquisável por igualdade. Ver [ADR-003](docs/adr/ADR-003-indice-cego-cpf.md).
- A API aceita tokens de **dois emissores**: o próprio login por e-mail e senha e a Function serverless de autenticação por CPF (`JwtSettings:IssuerCpf`), ambos assinados com a mesma chave simétrica.
- As rotas são geradas em minúsculas (`LowercaseUrls`), porque o roteamento do API Gateway é case-sensitive.
- Requisições vindas do API Gateway trazem `X-Correlation-Id`; o middleware de correlação coloca esse id no escopo de log e o devolve na resposta.
- Placas aceitam o formato antigo (`ABC1234`) e Mercosul (`ABC1D23`).
- Tokens JWT incluem claims de identificação, e-mail, nome de usuário, tipo de usuário e roles.
- A chave `JWT_SECRET_KEY` deve existir no ambiente de execução para geração e validação de tokens.
- Em ambiente de desenvolvimento, a API carrega User Secrets automaticamente.

## Testes

Execute todos os testes:

```bash
dotnet test
```

Os projetos de teste estão em `tests/` e cobrem regras de domínio, serviços de aplicação e endpoints HTTP.

| Projeto | Cobertura |
|---|---|
| `MechanicLtda.Domain.Tests` | Serviços de domínio e regras de negócio |
| `MechanicLtda.Application.Tests` | AppServices e autenticação |
| `MechanicLtda.API.IntegrationTests` | Controllers, autenticação JWT, autorização e fluxo HTTP com banco InMemory |
| `MechanicLtda.Web.Tests` | Clientes HTTP do Web para o OS Service e a Execução |

## Cobertura de Código

### Pré-requisito

Instale o `dotnet-reportgenerator-globaltool` caso ainda não tenha:

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool
```

### Gerar cobertura

```bash
dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory ./coverage-output
```

### Gerar relatório HTML

```bash
reportgenerator -reports:"./coverage-output/**/coverage.cobertura.xml" -targetdir:"./coverage-report" -reporttypes:Html -classfilters:"-MechanicLtda.Infrastructure.Migrations.*;-MechanicLtda.Infrastructure.FluentAPI.*" -filefilters:"-**/obj/**;-**/bin/**;-**/Debug/**;-**/Release/**"
```

### Abrir relatório no Windows

```powershell
start coverage-report\index.html
```

O relatório HTML será gerado em `coverage-report/`.

## Estrutura de Dependências

```text
API  → Bootstrap → Application → Domain
Web  → Bootstrap → Application → Domain
API  → Bootstrap → Infrastructure → Domain
Web  → Bootstrap → Infrastructure → Domain
```

A camada `Domain` é o núcleo da aplicação e não depende dos demais projetos da solução.
`Application` e `Infrastructure` dependem só de `Domain` — não uma da outra. `Bootstrap` é o
composition root compartilhado pelos dois hosts de apresentação (`API` e `Web`), evitando
duplicar a configuração de injeção de dependências, conexão com banco, migrations e seed entre
eles.

## Dados Iniciais (Seed)

Ao iniciar a aplicação com o banco vazio, os dados abaixo são inseridos automaticamente.

### Usuário administrador

| Campo | Valor |
|---|---|
| E-mail | `admin@mechanic.com` |
| Senha | `Admin@123` |
| Role | `Administrador` |

Use essas credenciais para realizar o primeiro login via `POST /api/auth/login`.

### Clientes

| Nome | E-mail | Telefone | CPF/CNPJ |
|---|---|---|---|
| Carlos Oliveira | `carlos@email.com` | `11999990001` | `123.456.789-01` |
| Fernanda Lima | `fernanda@email.com` | `11999990002` | `234.567.890-12` |
| Ricardo Souza | `ricardo@email.com` | `11999990003` | `345.678.901-23` |

### Veículos

| Placa | Marca | Modelo | Ano | Cliente |
|---|---|---|---|---|
| `ABC1D23` | Toyota | Corolla | 2021 | Carlos Oliveira |
| `DEF4E56` | Honda | Civic | 2019 | Fernanda Lima |
| `GHI7F89` | Volkswagen | Polo | 2022 | Ricardo Souza |
| `JKL0G12` | Chevrolet | Onix | 2020 | Carlos Oliveira |

Estoque e serviços da oficina agora são cadastrados na Execução. No compose de
`integracao/`, o serviço `carga-execucao` cadastra alguns itens de exemplo.

## Documentação da API

A collection completa dos endpoints é o próprio **Swagger/OpenAPI**, exposto pela API em
ambiente de desenvolvimento.

A partir da Fase 3 a API de produção fica atrás do **API Gateway**, e o Swagger passa a ser
acessado por `https://<api-id>.execute-api.us-east-1.amazonaws.com/swagger/index.html` (URL no
output `api_base_url` do repositório Lambda). O acesso direto à porta `8080` da instância só
responde até a virada para o gateway. Para consumir as rotas pelo gateway há uma **coleção
Postman** com testes em [`docs/postman`](docs/postman/README.md), cobrindo a autenticação por CPF
e por e-mail, as rotas protegidas e o fluxo completo da ordem de serviço. O roteiro do vídeo e o
PDF de entrega da Fase 3 estão em [`docs/entrega`](docs/entrega/README.md).

Endereços do Swagger:

- Local: `http://localhost:5062/swagger`
- Docker Compose: `http://localhost:8080/swagger`
- Produção: `http://<ip-publico-da-instancia>:8080/swagger`
