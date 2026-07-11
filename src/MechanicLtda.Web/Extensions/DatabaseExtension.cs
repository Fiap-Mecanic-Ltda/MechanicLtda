using MechanicLtda.Domain.Entities;
using MechanicLtda.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Web.Extensions
{
    public static class DatabaseExtension
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<BancoAPIContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<Usuario, IdentityRole>(options =>
            {
                options.Password.RequireDigit           = true;
                options.Password.RequiredLength         = 6;
                options.Password.RequireUppercase       = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail         = true;
            })
            .AddEntityFrameworkStores<BancoAPIContext>()
            .AddDefaultTokenProviders();

            return services;
        }
    }
}

