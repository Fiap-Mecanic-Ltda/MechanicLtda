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
            CreateMap<OrdemServicoUpdateDto, OrdemServico>();
            CreateMap<ItemOrdemServicoUpdateDto, ItemOrdemServico>();
            CreateMap<EstoqueCreateDto, Estoque>();
            CreateMap<EstoqueUpdateDto, Estoque>();
            CreateMap<ServicoOficinaCreateDto, ServicoOficina>();
            CreateMap<ServicoOficinaUpdateDto, ServicoOficina>();
            CreateMap<OrcamentoUpdateDto, Orcamento>();

            // Entidade → DTO  (Domain → Application)
            CreateMap<Usuario, UsuarioDto>();
            CreateMap<Cliente, ClienteDto>();
            CreateMap<Veiculo, VeiculoDto>();
            CreateMap<OrdemServico, OrdemServicoDto>()
                .ForMember(dest => dest.TempoExecucaoMinutos, opt => opt.MapFrom(src => src.TempoExecucao.HasValue ? src.TempoExecucao.Value.TotalMinutes : (double?)null));
            CreateMap<ItemOrdemServico, ItemOrdemServicoDto>();
            CreateMap<Orcamento, OrcamentoDto>();
            CreateMap<ServicoOficina, ServicoOficinaDto>();
        }
    }
}
