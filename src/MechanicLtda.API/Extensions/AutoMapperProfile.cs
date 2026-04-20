using AutoMapper;
using MechanicLtda.API.ViewModels;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;

namespace MechanicLtda.API.Extensions
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile() 
        {
            // ViewModel → DTO  (API → Application)
            CreateMap<UsuarioCreateViewModel, UsuarioCreateDto>();

            // DTO → Entidade  (Application → Domain)
            CreateMap<UsuarioCreateDto, Usuario>();

            // Entidade → DTO  (Domain → Application)
            CreateMap<Usuario, UsuarioDto>();
        }
    }
}
