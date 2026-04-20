# MechanicLtda

API REST desenvolvida em **.NET 9** para gerenciamento de uma mecânica, seguindo os princípios de **Clean Architecture** com separação em camadas bem definidas.

## Arquitetura

O projeto segue o padrão de **Arquitetura em Camadas (Layered / Clean Architecture)**:

```
MechanicLtda/
├── src/
│   ├── MechanicLtda.API            # Camada de apresentação (Controllers, configuração HTTP)
│   ├── MechanicLtda.Application    # Camada de aplicação (AppServices, DTOs)
│   ├── MechanicLtda.Domain         # Camada de domínio (Entidades, Interfaces, Serviços de domínio)
│   └── MechanicLtda.Infrastructure # Camada de infraestrutura (Repositórios, DbContext)
└── tests/
    ├── MechanicLtda.Application.Tests
    └── MechanicLtda.Domain.Tests
```

### Responsabilidades

| Projeto | Responsabilidade |
|---|---|
| `API` | Exposição dos endpoints HTTP, injeção de dependências |
| `Application` | Orquestração dos casos de uso via AppServices |
| `Domain` | Regras de negócio, interfaces de repositório e serviços de domínio |
| `Infrastructure` | Implementação dos repositórios e acesso ao banco de dados |

## Tecnologias

- [.NET 9](https://dotnet.microsoft.com/)
- ASP.NET Core Web API
- OpenAPI / Swagger (`Microsoft.AspNetCore.OpenApi`)
- Docker (suporte via `Dockerfile`)

## Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Docker (opcional, para rodar em container)

## Como executar

### Localmente

```bash
# Na raiz da solução
dotnet restore
dotnet build

cd src/MechanicLtda.API
dotnet run
```

A API estará disponível em:
- HTTP: `http://localhost:5062`
- HTTPS: `https://localhost:7013`

A documentação OpenAPI estará disponível em `http://localhost:5062/openapi` (ambiente de desenvolvimento).

### Com Docker

```bash
docker build -f src/MechanicLtda.API/Dockerfile -t mechanicltda-api .
docker run -p 8080:8080 -p 8081:8081 mechanicltda-api
```

## Testes

```bash
dotnet test
```

Os projetos de teste estão em `tests/` e cobrem as camadas `Application` e `Domain`.

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| — | `/cliente` | Gestão de clientes (em desenvolvimento) |

## Estrutura de dependências

```
API → Application → Domain
API → Infrastructure → Domain
```

A camada de `Domain` não possui dependências externas, sendo o núcleo da aplicação.
