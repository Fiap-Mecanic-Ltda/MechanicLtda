using MechanicLtda.Application.Mapping;

namespace MechanicLtda.API.Extensions
{
    public static class AutoMapperExtension
    {
        public static IServiceCollection AddAutoMapperProfiles(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<SharedAutoMapperProfile>();
                cfg.AddProfile<AutoMapperProfile>();
            });

            return services;
        }
    }
}