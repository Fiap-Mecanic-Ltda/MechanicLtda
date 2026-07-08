using AutoMapper;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;

namespace MechanicLtda.Web.Extensions
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<UsuarioCreateDto, Usuario>();
            CreateMap<UsuarioUpdateDto, Usuario>();
            CreateMap<ClienteUpdateDto, Cliente>();
            CreateMap<VeiculoUpdateDto, Veiculo>();
            CreateMap<OrdemServicoUpdateDto, OrdemServico>();
            CreateMap<ItemOrdemServicoUpdateDto, ItemOrdemServico>();
            CreateMap<EstoqueCreateDto, Estoque>();
            CreateMap<EstoqueUpdateDto, Estoque>();
            CreateMap<OrcamentoUpdateDto, Orcamento>();

            CreateMap<Usuario, UsuarioDto>();
            CreateMap<Cliente, ClienteDto>();
            CreateMap<Veiculo, VeiculoDto>();
            CreateMap<OrdemServico, OrdemServicoDto>();
            CreateMap<ItemOrdemServico, ItemOrdemServicoDto>();
            CreateMap<Orcamento, OrcamentoDto>();
        }
    }

    public static class AutoMapperExtension
    {
        public static IServiceCollection AddAutoMapperProfiles(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfile>());
            return services;
        }
    }
}

