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
            CreateMap<ClienteCreateViewModel, ClienteCreateDto>();
            CreateMap<ClienteUpdateViewModel, ClienteUpdateDto>();

            // DTO → Entidade  (Application → Domain)
            CreateMap<UsuarioCreateDto, Usuario>();
            CreateMap<ClienteUpdateDto, Cliente>();

            // Entidade → DTO  (Domain → Application)
            CreateMap<Usuario, UsuarioDto>();
            CreateMap<Cliente, ClienteDto>();
        }
    }
}
