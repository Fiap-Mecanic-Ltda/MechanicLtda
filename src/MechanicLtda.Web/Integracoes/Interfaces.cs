using MechanicLtda.Application.DTOs;

namespace MechanicLtda.Web.Integracoes
{
    /// <summary>Ordens de serviço no OrdemServico-service.</summary>
    public interface IOrdemServicoCliente
    {
        Task<ResponseDto<OrdemServicoDto>> AdicionarAsync(OrdemServicoCreateDto dto);
        Task<ResponseDto<OrdemServicoDto>> AtualizarAsync(string id, OrdemServicoUpdateDto dto);
        Task<ResponseDto<OrdemServicoDto>> IniciarDiagnosticoAsync(string id);
        Task<ResponseDto<OrdemServicoDto>> AguardarAprovacaoAsync(string id);
        Task<ResponseDto<OrdemServicoDto>> AprovarAsync(string id);
        Task<ResponseDto<OrdemServicoDto>> RecusarAsync(string id, string motivoRecusa);
        Task<ResponseDto<OrdemServicoDto>> IniciarExecucaoAsync(string id);
        Task<ResponseDto<OrdemServicoDto>> FinalizarAsync(string id);
        Task<ResponseDto<OrdemServicoDto>> EntregarAsync(string id);
        Task<ResponseDto<OrdemServicoDto>> CancelarAsync(int id, string? motivo);
        Task<ResponseDto<IEnumerable<OrdemServicoDto>>> ObterTodosAsync();
        Task<ResponseDto<TempoMedioExecucaoDto>> ObterTempoMedioExecucaoAsync();
        Task<ResponseDto<OrdemServicoDto>> ObterPorIdAsync(string id);
        Task<ResponseDto<IEnumerable<HistoricoStatusOrdemServicoDto>>> ObterHistoricoAsync(int id);
        Task<ResponseDto<bool>> RemoverAsync(string id);
    }

    /// <summary>
    /// Itens da OS no OrdemServico-service. Toda operação recebe a OS: no serviço o item só é
    /// endereçável dentro dela (/api/ordem-servico/{ordemServicoId}/itens/{id}).
    /// </summary>
    public interface IItensOrdemServicoCliente
    {
        Task<ResponseDto<IEnumerable<ItemOrdemServicoDto>>> ListarAsync(int ordemServicoId);
        Task<ResponseDto<ItemOrdemServicoDto>> ObterAsync(int ordemServicoId, int id);
        Task<ResponseDto<ItemOrdemServicoDto>> AdicionarAsync(int ordemServicoId, ItemOrdemServicoCreateDto dto);
        Task<ResponseDto<ItemOrdemServicoDto>> AtualizarAsync(int ordemServicoId, int id, ItemOrdemServicoUpdateDto dto);
        Task<ResponseDto<bool>> RemoverAsync(int ordemServicoId, int id);
    }

    /// <summary>Estoque de peças e insumos no ExecucaoProducao-service.</summary>
    public interface IEstoqueCliente
    {
        Task<ResponseDto<EstoqueDto>> AdicionarAsync(EstoqueCreateDto dto);
        Task<ResponseDto<EstoqueDto>> AtualizarAsync(string id, EstoqueUpdateDto dto);
        Task<ResponseDto<bool>> RemoverAsync(string id);
        Task<ResponseDto<IEnumerable<EstoqueDto>>> ObterTodosAsync();
        Task<ResponseDto<EstoqueDto>> ObterPorIdAsync(string id);
        Task<ResponseDto<EstoqueDto>> ReporQuantidadeAsync(string id, EstoqueReposicaoDto dto);
    }

    /// <summary>Catálogo de serviços da oficina no ExecucaoProducao-service.</summary>
    public interface IServicoOficinaCliente
    {
        Task<ResponseDto<ServicoOficinaDto>> AdicionarAsync(ServicoOficinaCreateDto dto);
        Task<ResponseDto<ServicoOficinaDto>> AtualizarAsync(string id, ServicoOficinaUpdateDto dto);
        Task<ResponseDto<bool>> RemoverAsync(string id);
        Task<ResponseDto<IEnumerable<ServicoOficinaDto>>> ObterTodosAsync();
        Task<ResponseDto<IEnumerable<ServicoOficinaDto>>> ObterAtivosAsync();
        Task<ResponseDto<ServicoOficinaDto>> ObterPorIdAsync(string id);
    }
}
