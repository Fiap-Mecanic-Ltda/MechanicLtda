using AutoMapper;
using MechanicLtda.API.ViewModels;
using MechanicLtda.Application.DTOs;

namespace MechanicLtda.API.Extensions
{
    /// <summary>
    /// Mapeamentos exclusivos da API: ViewModel → DTO. Os mapeamentos DTO ↔ Entidade,
    /// compartilhados com outros hosts, estão em MechanicLtda.Application.Mapping.SharedAutoMapperProfile.
    /// </summary>
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<UsuarioCreateViewModel, UsuarioCreateDto>();
            CreateMap<UsuarioUpdateViewModel, UsuarioUpdateDto>();
            CreateMap<ClienteCreateViewModel, ClienteCreateDto>();
            CreateMap<ClienteUpdateViewModel, ClienteUpdateDto>();
            CreateMap<VeiculoCreateViewModel, VeiculoCreateDto>();
            CreateMap<VeiculoUpdateViewModel, VeiculoUpdateDto>();
            CreateMap<OrdemServicoCreateViewModel, OrdemServicoCreateDto>();
            CreateMap<OrdemServicoUpdateViewModel, OrdemServicoUpdateDto>();
            CreateMap<ItemOrdemServicoCreateViewModel, ItemOrdemServicoCreateDto>();
            CreateMap<ItemOrdemServicoUpdateViewModel, ItemOrdemServicoUpdateDto>();
            CreateMap<EstoqueCreateViewModel, EstoqueCreateDto>();
            CreateMap<EstoqueUpdateViewModel, EstoqueUpdateDto>();
            CreateMap<EstoqueReposicaoViewModel, EstoqueReposicaoDto>();
            CreateMap<ServicoOficinaCreateViewModel, ServicoOficinaCreateDto>();
            CreateMap<ServicoOficinaUpdateViewModel, ServicoOficinaUpdateDto>();
            CreateMap<OrcamentoUpdateViewModel, OrcamentoUpdateDto>();
        }
    }
}

