using MechanicLtda.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add User Secrets in development environment
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithAuth();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddDependencyInjection();
builder.Services.AddAutoMapperProfiles();
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

// 1. Cria o banco e aplica migrations — deve ser o primeiro passo
await app.MigrateDatabaseAsync();

// 2. Seed de roles — depende do schema existir
await app.SeedRolesAsync();

// 3. Seed de dados mocados — depende dos roles existirem
await app.SeedDataAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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