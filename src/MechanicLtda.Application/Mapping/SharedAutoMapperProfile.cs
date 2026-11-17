using AutoMapper;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;

namespace MechanicLtda.Application.Mapping
{
    /// <summary>
    /// Mapeamentos DTO ↔ Entidade compartilhados entre todos os hosts (API e Web) —
    /// eles expõem esses DTOs da mesma forma, independentemente da camada de apresentação.
    /// Mapeamentos específicos de ViewModel de cada host ficam no profile local de cada projeto.
    /// </summary>
    public class SharedAutoMapperProfile : Profile
    {
        public SharedAutoMapperProfile()
        {
            // DTO → Entidade  (Application → Domain)
            CreateMap<UsuarioCreateDto, Usuario>();
            CreateMap<UsuarioUpdateDto, Usuario>();
            CreateMap<ClienteUpdateDto, Cliente>();
            CreateMap<VeiculoUpdateDto, Veiculo>();

            // Entidade → DTO  (Domain → Application)
            CreateMap<Usuario, UsuarioDto>();
            CreateMap<Cliente, ClienteDto>();
            CreateMap<Veiculo, VeiculoDto>();
        }
    }
}
