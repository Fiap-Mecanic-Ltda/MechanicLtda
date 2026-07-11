using MechanicLtda.Application.Mapping;

namespace MechanicLtda.Web.Extensions
{
    /// <summary>
    /// O Web ainda não usa AutoMapper para ViewModel → DTO (mapeamento feito manualmente
    /// nos controllers); por isso só registra o profile DTO ↔ Entidade compartilhado com a API.
    /// </summary>
    public static class AutoMapperExtension
    {
        public static IServiceCollection AddAutoMapperProfiles(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg => cfg.AddProfile<SharedAutoMapperProfile>());
            return services;
        }
    }
}


