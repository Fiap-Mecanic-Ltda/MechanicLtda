# MechanicLtda

Sistema de gestão para uma oficina mecânica (ordens de serviço, veículos, clientes, estoque e
orçamentos), desenvolvido em **.NET 9** seguindo princípios de DDD, Clean Architecture e
Clean Code.

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

## Sobre esta fase (Tech Challenge — Fase 2)

A Fase 1 entregou a API de gestão da oficina. Esta fase evolui essa base para suportar alta
disponibilidade e volume de pico, sem alterar as regras de negócio já existentes:

- **Escalabilidade e resiliência**: containerização com Docker, orquestração em Kubernetes com
  Horizontal Pod Autoscaler (HPA) por CPU/memória.
- **Infraestrutura como código**: todo o ambiente (rede, banco, containers, registry, IAM) é
  provisionado via Terraform, sem passos manuais no console da AWS.
- **Automação do provisionamento e do deploy**: pipeline de CI/CD (GitHub Actions) cobrindo
  build, testes, build/push da imagem Docker e deploy no cluster Kubernetes a cada push na
  `main`.
- **Novas funcionalidades na Ordem de Serviço**: aprovação/recusa de orçamento por link de
  e-mail, notificação automática de mudança de status por e-mail, e listagem com ordenação por
  prioridade de status e exclusão lógica das OS já concluídas.

## Arquitetura

### Componentes da aplicação

O projeto segue o padrão de **Arquitetura em Camadas (Layered / Clean Architecture)**, com dois
hosts de apresentação (API REST e um front-end Web MVC) compartilhando as mesmas camadas de
domínio e aplicação:

```text
MechanicLtda/
├── src/
│   ├── MechanicLtda.API            # Host REST: Controllers, ViewModels, Swagger, JWT
│   ├── MechanicLtda.Web             # Host MVC: Controllers, Views Razor, autenticação por cookie
│   ├── MechanicLtda.Bootstrap      # Composition root compartilhado por API e Web (DI, DB, migrations, seed)
│   ├── MechanicLtda.Application    # AppServices, DTOs, autenticação e geração de PDF
│   ├── MechanicLtda.Domain         # Entidades, enums, interfaces e serviços de domínio
│   └── MechanicLtda.Infrastructure # Repositórios, DbContext, Fluent API, migrations, e-mail e criptografia
├── tests/
│   ├── MechanicLtda.API.IntegrationTests
│   ├── MechanicLtda.Application.Tests
│   └── MechanicLtda.Domain.Tests
└── .github/workflows/               # Pipeline de CI/CD (GitHub Actions): build, testes e push no ECR
```

### Responsabilidades

| Projeto | Responsabilidade |
|---|---|
| `API` | Exposição dos endpoints HTTP, validações, Swagger, autenticação JWT e autorização por roles |
| `Web` | Interface administrativa via Razor Views, autenticação por cookie |
| `Bootstrap` | Composition root: injeção de dependências, conexão com banco, migrations e seed — compartilhado por `API` e `Web` para não duplicar esse código entre os dois hosts |
| `Application` | Orquestração dos casos de uso, DTOs, AutoMapper, login, troca de senha e exportação de PDF |
| `Domain` | Regras de negócio, entidades, enums, contratos de repositórios e serviços de domínio |
| `Infrastructure` | EF Core, SQL Server, Identity, migrations, repositórios, envio de e-mail (MailKit) e criptografia de CPF/CNPJ |

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
- QuestPDF para exportação de orçamentos em PDF
- MailKit/SMTP para notificação e aprovação de Ordens de Serviço por e-mail
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

## Modelo de Domínio

| Entidade | Descrição |
|---|---|
| `Usuario` | Usuário do sistema, baseado em Identity, com tipo e estado ativo/inativo |
| `Cliente` | Cliente da mecânica, com CPF/CNPJ validado e criptografado no banco |
| `Veiculo` | Veículo vinculado a um cliente, com validação de placa antiga ou Mercosul |
| `OrdemServico` | Ordem de serviço vinculada a cliente e veículo, com status, problema e valor estimado |
| `ItemOrdemServico` | Item de uma ordem de serviço, podendo consumir estoque ou referenciar um serviço cadastrado |
| `Estoque` | Peça ou insumo gerenciado em estoque |
| `ServicoOficina` | Catálogo de serviços prestados pela oficina, com descrição, valor base e estado ativo/inativo |
| `Orcamento` | Orçamento gerado e recalculado a partir dos itens da ordem de serviço |

### Enums principais

| Enum | Valores |
|---|---|
| `TipoUsuario` | `1 = Administrador`, `2 = Funcionario`, `3 = Cliente` |
| `TipoEstoque` | `1 = Insumo`, `2 = Peca` |
| `StatusOrdemServico` | `1 = Recebida`, `2 = EmDiagnostico`, `3 = AguardandoAprovacao`, `4 = EmExecucao`, `5 = Finalizada`, `6 = Entregue` |

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
    "Audience": "MechanicLtda.Clients",
    "ExpiracaoMinutos": 60
  },
  "Encryption": {
    "CpfCnpjKey": "sua-chave-de-criptografia-com-no-minimo-32-caracteres"
  }
}
```

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
```

| Variável | Uso |
|---|---|
| `SA_PASSWORD` | Senha do usuário `sa` do SQL Server |
| `JWT_SECRET_KEY` | Chave secreta para assinatura e validação dos tokens JWT |
| `ENCRYPTION_KEY` | Chave de criptografia dos campos CPF/CNPJ |

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
| `Cliente` | Consulta de progresso das próprias ordens de serviço |

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

### Ordens de Serviço

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/ordemservico` | Administrador, Funcionario | Lista as OS em andamento, ordenadas por prioridade de status (Execução primeiro) e, dentro do mesmo status, mais antigas primeiro. Finalizadas/Entregues não aparecem aqui (exclusão lógica) |
| `GET` | `/api/ordemservico/{id}` | Administrador, Funcionario | Retorna uma ordem de serviço pelo Id (inclui Finalizadas/Entregues) |
| `GET` | `/api/ordemservico/cliente/{clienteId}` | Administrador, Funcionario, Cliente | Lista ordens de serviço por cliente |
| `POST` | `/api/ordemservico` | Administrador, Funcionario | Cria uma ordem de serviço com status inicial `Recebida` |
| `PUT` | `/api/ordemservico/{id}` | Administrador, Funcionario | Atualiza uma ordem de serviço |
| `PATCH` | `/api/ordemservico/{id}/iniciar-diagnostico` | Administrador, Funcionario | Move a OS para `EmDiagnostico` |
| `PATCH` | `/api/ordemservico/{id}/aguardar-aprovacao` | Administrador, Funcionario | Move a OS para `AguardandoAprovacao` — dispara e-mail de notificação e o link de aprovação/recusa para o cliente |
| `PATCH` | `/api/ordemservico/{id}/aprovar` | Administrador, Funcionario | Aprova a OS (`AguardandoAprovacao` → `EmExecucao`) |
| `PATCH` | `/api/ordemservico/{id}/recusar` | Administrador, Funcionario | Recusa a OS (`AguardandoAprovacao` → `EmDiagnostico`), com motivo opcional |
| `PATCH` | `/api/ordemservico/{id}/iniciar-execucao` | Administrador, Funcionario | Move a OS para `EmExecucao` |
| `PATCH` | `/api/ordemservico/{id}/finalizar` | Administrador, Funcionario | Move a OS para `Finalizada` |
| `PATCH` | `/api/ordemservico/{id}/entregar` | Administrador, Funcionario | Move a OS para `Entregue` |
| `DELETE` | `/api/ordemservico/{id}` | Administrador, Funcionario | Remove uma ordem de serviço |

Toda transição de status dispara um e-mail de notificação ao cliente (quando ele tem e-mail
cadastrado). Ao entrar em `AguardandoAprovacao`, o e-mail inclui links de aprovação/recusa que
não exigem login — ver seção seguinte.

### Aprovação de Orçamento via E-mail

Endpoint público (sem autenticação), acessado pelo link enviado por e-mail ao cliente quando a
OS entra em `AguardandoAprovacao`. O token expira e só pode ser usado uma vez.

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/AprovacaoOrdemServico/{token}/aprovar` | Público (token) | Aprova a OS associada ao token |
| `GET` | `/api/AprovacaoOrdemServico/{token}/recusar` | Público (token) | Recusa a OS associada ao token |

### Serviços da Oficina

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/servicos-oficina` | Administrador, Funcionario | Lista todos os serviços cadastrados |
| `GET` | `/api/servicos-oficina/ativos` | Administrador, Funcionario | Lista apenas os serviços ativos |
| `GET` | `/api/servicos-oficina/{id}` | Administrador, Funcionario | Retorna um serviço pelo Id |
| `POST` | `/api/servicos-oficina` | Administrador, Funcionario | Cadastra um novo serviço |
| `PUT` | `/api/servicos-oficina/{id}` | Administrador, Funcionario | Atualiza um serviço |
| `DELETE` | `/api/servicos-oficina/{id}` | Administrador, Funcionario | Remove um serviço |

### Itens de Ordem de Serviço

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/ordem-servico/{ordemServicoId}/itens` | Administrador, Funcionario | Lista itens de uma ordem de serviço |
| `GET` | `/api/ordem-servico/{ordemServicoId}/itens/{id}` | Administrador, Funcionario | Retorna um item pelo Id |
| `POST` | `/api/ordem-servico/{ordemServicoId}/itens` | Administrador, Funcionario | Adiciona um item e recalcula a OS |
| `PUT` | `/api/ordem-servico/{ordemServicoId}/itens/{id}` | Administrador, Funcionario | Atualiza um item e recalcula a OS |
| `DELETE` | `/api/ordem-servico/{ordemServicoId}/itens/{id}` | Administrador, Funcionario | Remove um item e recalcula a OS |

### Estoque

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/estoque` | Administrador, Funcionario | Lista todos os itens de estoque |
| `GET` | `/api/estoque/{id}` | Administrador, Funcionario | Retorna um item de estoque pelo Id |
| `GET` | `/api/estoque/tipo/{tipo}` | Administrador, Funcionario | Filtra itens por tipo (`1 = Insumo`, `2 = Peca`) |
| `POST` | `/api/estoque` | Administrador, Funcionario | Cadastra um novo item no estoque |
| `PUT` | `/api/estoque/{id}` | Administrador, Funcionario | Atualiza dados cadastrais do item |
| `PATCH` | `/api/estoque/{id}/reposicao` | Administrador, Funcionario | Reposição de quantidade em estoque |
| `DELETE` | `/api/estoque/{id}` | Administrador, Funcionario | Remove um item de estoque |

### Orçamentos

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/orcamento/{id}` | Administrador, Funcionario | Retorna um orçamento pelo Id |
| `GET` | `/api/orcamento/ordem-servico/{ordemServicoId}` | Administrador, Funcionario | Retorna o orçamento de uma ordem de serviço |
| `PUT` | `/api/orcamento/{id}` | Administrador, Funcionario | Atualiza manualmente valores do orçamento |
| `DELETE` | `/api/orcamento/{id}` | Administrador, Funcionario | Remove um orçamento |
| `GET` | `/api/orcamento/{id}/pdf` | Administrador, Funcionario | Exporta o orçamento em PDF |

Os orçamentos são gerados e atualizados automaticamente ao adicionar, atualizar ou remover itens de uma ordem de serviço.

## Validações e Segurança

- CPF/CNPJ de clientes é validado na API e criptografado no banco via `CpfCnpjEncryptionConverter`.
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
| `MechanicLtda.Application.Tests` | AppServices, autenticação, orçamento e exportação PDF |
| `MechanicLtda.API.IntegrationTests` | Controllers, autenticação JWT, autorização e fluxo HTTP com banco InMemory |

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

### Estoque

| Nome | Tipo | Qtd. Atual | Qtd. Mínima |
|---|---|---|---|
| Óleo Motor 5W30 | Insumo | 50 | 10 |
| Filtro de Ar | Peça | 30 | 5 |
| Pastilha de Freio | Peça | 20 | 4 |
| Fluido de Freio DOT 4 | Insumo | 15 | 3 |
| Correia Dentada | Peça | 8 | 2 |

### Ordens de Serviço

| Cliente | Veículo | Problema | Status | Valor Estimado |
|---|---|---|---|---|
| Carlos Oliveira | Toyota Corolla | Troca de óleo e filtro de ar. | Em Execução | R$ 250,00 |
| Fernanda Lima | Honda Civic | Revisão de freios dianteiros e traseiros. | Recebida | R$ 420,00 |
| Ricardo Souza | Volkswagen Polo | Substituição de correia dentada. | Aguardando Aprovação | R$ 680,00 |

Os itens das ordens de serviço e os orçamentos correspondentes também são gerados automaticamente no seed.

## Documentação da API

A collection completa dos endpoints é o próprio **Swagger/OpenAPI**, exposto pela API em
ambiente de desenvolvimento:

- Local: `http://localhost:5062/swagger`
- Docker Compose: `http://localhost:8080/swagger`
- Produção: `http://<ip-publico-da-instancia>:8080/swagger`
