using MechanicLtda.Bootstrap.Extensions;
using MechanicLtda.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Logs em JSON fora do ambiente de desenvolvimento (mesmo formato da API).
builder.AddLogsEstruturados();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.AddControllersWithViews();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddDependencyInjection();
builder.Services.AddAutoMapperProfiles();
builder.Services.AddCookieAuthentication();

var app = builder.Build();

await app.MigrateDatabaseAsync();
await app.SeedRolesAsync();
await app.SeedDataAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Usado pelo healthcheck do Docker Compose.
app.MapGet("/health", () => Results.Ok()).AllowAnonymous();

app.Run();

public partial class Program { }

