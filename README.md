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
- Docker + Docker Compose (opcional, para rodar em container)

## Configuração

### Localmente

Configure a string de conexão e as configurações JWT no arquivo `appsettings.json` (ou via variáveis de ambiente):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=MechanicLtda;..."
  },
  "JwtSettings": {
    "SecretKey": "sua-chave-secreta",
    "Issuer": "MechanicLtda.API",
    "Audience": "MechanicLtda.Clients",
    "ExpiracaoMinutos": 60
  },
  "Encryption": {
    "CpfCnpjKey": "sua-chave-de-criptografia"
  }
}
```

### Com Docker (variáveis de ambiente / secrets)

O `docker-compose.yml` utiliza variáveis de ambiente para injetar credenciais sensíveis nos serviços, evitando que segredos fiquem hardcoded na imagem ou no repositório.

Crie um arquivo `.env` na raiz do projeto com o seguinte conteúdo:

```env
SA_PASSWORD=SuaSenhaForte@123
JWT_SECRET_KEY=sua-chave-jwt-secreta-com-no-minimo-32-caracteres
ENCRYPTION_KEY=sua-chave-de-criptografia-32chars
```

> **Importante:** nunca commite o arquivo `.env` no repositório. Ele já está listado no `.gitignore`.

As variáveis são utilizadas da seguinte forma:

| Variável | Uso |
|---|---|
| `SA_PASSWORD` | Senha do usuário `sa` do SQL Server |
| `JWT_SECRET_KEY` | Chave secreta para assinatura dos tokens JWT |
| `ENCRYPTION_KEY` | Chave de criptografia dos campos CPF/CNPJ |

## Como executar

### Localmente

```bash
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

### Com Docker Compose

Certifique-se de ter criado o arquivo `.env` conforme descrito na seção de configuração, depois execute:

```bash
docker compose up --build
```

Isso irá:
1. Subir o container do **SQL Server 2022** (`mechanicltda-sqlserver`) na porta `1433`
2. Aguardar o SQL Server ficar saudável (healthcheck automático)
3. Construir e subir o container da **API** (`mechanicltda-api`) na porta `8080`

A API estará disponível em `http://localhost:8080` e o Swagger em `http://localhost:8080/swagger`.

Para parar os serviços:

```bash
docker compose down
```

Para remover também o volume de dados do SQL Server:

```bash
docker compose down -v
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
reportgenerator -reports:"./coverage-output/**/coverage.cobertura.xml" -targetdir:"./coverage-report" -reporttypes:Html
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

## Dados Iniciais (Seed)

Ao iniciar a aplicação pela primeira vez com o banco vazio, os seguintes dados são inseridos automaticamente.

### Usuário administrador

| Campo | Valor |
|---|---|
| **E-mail** | `admin@mechanic.com` |
| **Senha** | `Admin@123` |
| **Role** | `Administrador` |

> Use essas credenciais para realizar o primeiro login via `POST /api/auth/login`.

### Clientes

| Nome | E-mail | Telefone | CPF/CNPJ |
|---|---|---|---|
| Carlos Oliveira | carlos@email.com | 11999990001 | 123.456.789-01 |
| Fernanda Lima | fernanda@email.com | 11999990002 | 234.567.890-12 |
| Ricardo Souza | ricardo@email.com | 11999990003 | 345.678.901-23 |

### Veículos

| Placa | Marca | Modelo | Ano | Cliente |
|---|---|---|---|---|
| ABC1D23 | Toyota | Corolla | 2021 | Carlos Oliveira |
| DEF4E56 | Honda | Civic | 2019 | Fernanda Lima |
| GHI7F89 | Volkswagen | Polo | 2022 | Ricardo Souza |
| JKL0G12 | Chevrolet | Onix | 2020 | Carlos Oliveira |

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

> Os orçamentos correspondentes a cada ordem de serviço também são gerados automaticamente no seed.