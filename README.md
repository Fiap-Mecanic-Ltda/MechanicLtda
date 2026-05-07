# MechanicLtda

API REST desenvolvida em **.NET 9** para gerenciamento de uma mecânica, seguindo os princípios de DDD com separação em camadas bem definidas.

## Arquitetura

O projeto segue o padrão de **Arquitetura em Camadas (Layered / Clean Architecture)**:

```
MechanicLtda/
├── src/
│   ├── MechanicLtda.API            # Camada de apresentação (Controllers, Extensions, ViewModels)
│   ├── MechanicLtda.Application    # Camada de aplicação (AppServices, DTOs)
│   ├── MechanicLtda.Domain         # Camada de domínio (Entidades, Interfaces, Serviços de domínio)
│   └── MechanicLtda.Infrastructure # Camada de infraestrutura (Repositórios, DbContext, Migrations)
└── tests/
    ├── MechanicLtda.Application.Tests
    └── MechanicLtda.Domain.Tests
```

### Responsabilidades

| Projeto | Responsabilidade |
|---|---|
| `API` | Exposição dos endpoints HTTP, injeção de dependências, autenticação JWT |
| `Application` | Orquestração dos casos de uso via AppServices, mapeamento com AutoMapper |
| `Domain` | Regras de negócio, interfaces de repositório e serviços de domínio |
| `Infrastructure` | Implementação dos repositórios, DbContext (EF Core + SQL Server), Migrations |

## Tecnologias

- [.NET 9](https://dotnet.microsoft.com/)
- ASP.NET Core Web API
- Entity Framework Core (SQL Server)
- ASP.NET Core Identity (`IdentityDbContext<Usuario>`)
- Autenticação JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- AutoMapper
- Swagger / OpenAPI com suporte a Bearer Token (`Swashbuckle`)
- xUnit + Moq (testes)
- Docker (suporte via `Dockerfile`)

## Modelo de Domínio

| Entidade | Descrição |
|---|---|
| `Usuario` | Usuário do sistema (Identity) com tipo e estado ativo/inativo |
| `Cliente` | Cliente da mecânica |
| `Veiculo` | Veículo vinculado a um cliente |
| `OrdemServico` | Ordem de serviço vinculada a cliente e veículo, com status e valor estimado |
| `ItemOrdemServico` | Item de uma ordem de serviço, podendo ser vinculado ao estoque |
| `Estoque` | Peça ou insumo gerenciado em estoque |
| `Orcamento` | Orçamento gerado automaticamente a partir de uma ordem de serviço |

## Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server (local ou remoto)
- Docker (opcional, para rodar em container)

## Configuração

Configure a string de conexão e as configurações JWT no arquivo `appsettings.json` (ou via variáveis de ambiente):

```
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=MechanicLtda;..."
  },
  "JwtSettings": {
    "SecretKey": "sua-chave-secreta",
    "Issuer": "MechanicLtda",
    "Audience": "MechanicLtdaUsers",
    "ExpiracaoMinutos": 60
  }
}
```

## Como executar

### Localmente

```
# Na raiz da solução
dotnet restore
dotnet build

# Aplicar migrations (banco de dados)
dotnet ef database update --project src/MechanicLtda.Infrastructure --startup-project src/MechanicLtda.API

cd src/MechanicLtda.API
dotnet run
```

A API estará disponível em:
- HTTP: `http://localhost:5062`
- HTTPS: `https://localhost:7013`

A documentação Swagger estará disponível em `http://localhost:5062/swagger` (ambiente de desenvolvimento).

### Com Docker

```
docker build -f src/MechanicLtda.API/Dockerfile -t mechanicltda-api .
docker run -p 8080:8080 -p 8081:8081 mechanicltda-api
```

## Autenticação

A API utiliza **JWT Bearer Token**. Para acessar os endpoints protegidos:

1. Realize o login via `POST /api/auth/login` informando `email` e `senha`.
2. Copie o token retornado.
3. No Swagger, clique em **Authorize** e informe: `Bearer {seu_token}`.

> Todos os endpoints (exceto `/api/auth/login`) requerem autenticação.

## Endpoints

### Auth

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| `POST` | `/api/auth/login` | ❌ | Realiza login e retorna o token JWT |
| `PUT` | `/api/auth/alterar-senha` | ✅ | Altera a senha do usuário autenticado |

### Usuários

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| `GET` | `/api/usuario` | ✅ | Lista todos os usuários |
| `POST` | `/api/usuario` | ✅ | Cria um novo usuário |
| `PUT` | `/api/usuario/{id}` | ✅ | Atualiza um usuário |
| `DELETE` | `/api/usuario/{id}` | ✅ | Remove um usuário |

### Clientes

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| `GET` | `/api/cliente` | ✅ | Lista todos os clientes |
| `GET` | `/api/cliente/{id}` | ✅ | Retorna um cliente pelo Id |
| `POST` | `/api/cliente` | ✅ | Cria um novo cliente |
| `PUT` | `/api/cliente/{id}` | ✅ | Atualiza um cliente |
| `DELETE` | `/api/cliente/{id}` | ✅ | Remove um cliente |

### Veículos

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| `GET` | `/api/veiculo` | ✅ | Lista todos os veículos |
| `GET` | `/api/veiculo/{id}` | ✅ | Retorna um veículo pelo Id |
| `GET` | `/api/veiculo/cliente/{clienteId}` | ✅ | Lista veículos de um cliente |
| `POST` | `/api/veiculo` | ✅ | Cria um novo veículo |
| `PUT` | `/api/veiculo/{id}` | ✅ | Atualiza um veículo |
| `DELETE` | `/api/veiculo/{id}` | ✅ | Remove um veículo |

### Ordens de Serviço

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| `GET` | `/api/ordemservico` | ✅ | Lista todas as ordens de serviço |
| `GET` | `/api/ordemservico/{id}` | ✅ | Retorna uma ordem de serviço pelo Id |
| `GET` | `/api/ordemservico/cliente/{clienteId}` | ✅ | Lista ordens de serviço por cliente |
| `POST` | `/api/ordemservico` | ✅ | Cria uma nova ordem de serviço |
| `PUT` | `/api/ordemservico/{id}` | ✅ | Atualiza uma ordem de serviço |
| `PATCH` | `/api/ordemservico/{id}/validacao` | ✅ | Move a ordem de serviço para "Em Validação" |
| `DELETE` | `/api/ordemservico/{id}` | ✅ | Remove uma ordem de serviço |

### Itens de Ordem de Serviço

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| `GET` | `/api/ordemservico/{ordemServicoId}/itens` | ✅ | Lista itens de uma ordem de serviço |
| `POST` | `/api/ordemservico/{ordemServicoId}/itens` | ✅ | Adiciona um item (desconta estoque automaticamente) |
| `PUT` | `/api/ordemservico/{ordemServicoId}/itens/{id}` | ✅ | Atualiza um item |
| `DELETE` | `/api/ordemservico/{ordemServicoId}/itens/{id}` | ✅ | Remove um item |

### Estoque

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| `GET` | `/api/estoque` | ✅ | Lista todos os itens de estoque |
| `GET` | `/api/estoque/{id}` | ✅ | Retorna um item de estoque pelo Id |
| `GET` | `/api/estoque/tipo/{tipo}` | ✅ | Filtra itens por tipo (`1` = Insumo, `2` = Peça) |
| `POST` | `/api/estoque` | ✅ | Cadastra um novo item no estoque |
| `PUT` | `/api/estoque/{id}` | ✅ | Atualiza um item de estoque |
| `DELETE` | `/api/estoque/{id}` | ✅ | Remove um item de estoque |

### Orçamentos

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| `GET` | `/api/orcamento/{id}` | ✅ | Retorna um orçamento pelo Id |
| `GET` | `/api/orcamento/ordemservico/{ordemServicoId}` | ✅ | Retorna o orçamento de uma ordem de serviço |
| `PUT` | `/api/orcamento/{id}` | ✅ | Atualiza manualmente um orçamento |
| `DELETE` | `/api/orcamento/{id}` | ✅ | Remove um orçamento |

> Os orçamentos são **gerados e atualizados automaticamente** ao adicionar, atualizar ou remover itens de uma ordem de serviço.

## Testes

```
dotnet test
```

Os projetos de teste estão em `tests/` e utilizam **xUnit** e **Moq**, cobrindo as camadas `Application` e `Domain`.

| Projeto | Cobertura |
|---|---|
| `MechanicLtda.Application.Tests` | AppServices (ex: `OrcamentoAppService`) |
| `MechanicLtda.Domain.Tests` | Serviços de domínio (ex: `ItemOrdemServicoService`) |

## Cobertura de código

### Pré-requisito

Instale o `dotnet-reportgenerator-globaltool` caso ainda não tenha:

```
dotnet tool install --global dotnet-reportgenerator-globaltool
```

### Gerar o relatório

Execute os comandos abaixo na raiz da solução:

```
dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory ./coverage-output
```
### Gerar o relatório HTML

```
reportgenerator -reports:"./coverage-output/**/coverage.cobertura.xml" -targetdir:"./coverage-report" -reporttypes:Html -classfilters:"-MechanicLtda.Infrastructure.Migrations.*;-MechanicLtda.Infrastructure.FluentAPI.*" -filefilters:"-**/obj/**;-**/bin/**;-**/Debug/**;-**/Release/**"
```

### Abrir o relatório (Windows)

```
start coverage-report\index.html
```

O relatório HTML será gerado na pasta `coverage-report/` e pode ser aberto em qualquer navegador.

## Estrutura de dependências

```
API → Application → Domain
API → Infrastructure → Domain
```

A camada de `Domain` não possui dependências externas, sendo o núcleo da aplicação.
