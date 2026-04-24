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
            CreateMap<VeiculoCreateViewModel, VeiculoCreateDto>();
            CreateMap<VeiculoUpdateViewModel, VeiculoUpdateDto>();
            CreateMap<OrdemServicoCreateViewModel, OrdemServicoCreateDto>();
            CreateMap<OrdemServicoUpdateViewModel, OrdemServicoUpdateDto>();
            CreateMap<ItemOrdemServicoCreateViewModel, ItemOrdemServicoCreateDto>();
            CreateMap<ItemOrdemServicoUpdateViewModel, ItemOrdemServicoUpdateDto>();
            CreateMap<EstoqueCreateViewModel, EstoqueCreateDto>();
            CreateMap<EstoqueUpdateViewModel, EstoqueUpdateDto>();
            CreateMap<EstoqueReposicaoViewModel, EstoqueReposicaoDto>();
            CreateMap<OrcamentoUpdateViewModel, OrcamentoUpdateDto>();

            // DTO → Entidade  (Application → Domain)
            CreateMap<UsuarioCreateDto, Usuario>();
            CreateMap<ClienteUpdateDto, Cliente>();
            CreateMap<VeiculoUpdateDto, Veiculo>();
            CreateMap<OrdemServicoUpdateDto, OrdemServico>();
            CreateMap<ItemOrdemServicoUpdateDto, ItemOrdemServico>();
            CreateMap<EstoqueCreateDto, Estoque>();
            CreateMap<EstoqueUpdateDto, Estoque>();
            CreateMap<OrcamentoUpdateDto, Orcamento>();

            // Entidade → DTO  (Domain → Application)
            CreateMap<Usuario, UsuarioDto>();
            CreateMap<Cliente, ClienteDto>();
            CreateMap<Veiculo, VeiculoDto>();
            CreateMap<OrdemServico, OrdemServicoDto>();
            CreateMap<ItemOrdemServico, ItemOrdemServicoDto>();
            CreateMap<Orcamento, OrcamentoDto>();
        }
    }
}
