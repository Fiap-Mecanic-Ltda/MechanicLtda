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

await app.SeedRolesAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Expõe a classe Program para WebApplicationFactory
public partial class Program { }