using MechanicLtda.API.Extensions;
using MechanicLtda.API.Middlewares;
using MechanicLtda.Bootstrap.Extensions;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add User Secrets in development environment
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Add services to the container.

builder.Services.AddControllers();

// Rotas em minúsculas: o roteamento do API Gateway é case-sensitive, então Swagger,
// Postman e as rotas declaradas no gateway precisam falar exatamente a mesma grafia.
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// A API passa a receber tráfego pelo API Gateway e por um ALB interno. Sem confiar nos
// headers de encaminhamento, o ASP.NET vê o IP e o esquema do proxy (e não do cliente),
// e as URLs geradas saem com host interno.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                               | ForwardedHeaders.XForwardedProto
                               | ForwardedHeaders.XForwardedHost;

    // Os proxies são gerenciados (API Gateway e ALB) e têm IP dinâmico, por isso as
    // listas de redes/proxies conhecidos ficam vazias.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithAuth();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddDependencyInjection();
builder.Services.AddAutoMapperProfiles();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddEmailService(builder.Configuration);

var app = builder.Build();

// 1. Cria o banco e aplica migrations — deve ser o primeiro passo
await app.MigrateDatabaseAsync();

// 2. Seed de roles — depende do schema existir
await app.SeedRolesAsync();

// 3. Seed de dados mocados — depende dos roles existirem
await app.SeedDataAsync();

// 4. Índice cego do CPF/CNPJ das linhas antigas — depende do schema e do seed
await app.BackfillCpfCnpjHashAsync();

app.UseForwardedHeaders();
app.UseCorrelationId();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Usado pelo healthcheck do Docker Compose para sequenciar a subida do host Web
// (que espera a API concluir migrations/seed antes de iniciar).
app.MapGet("/health", () => Results.Ok()).AllowAnonymous();

app.Run();

// Expõe a classe Program para WebApplicationFactory
public partial class Program { }
