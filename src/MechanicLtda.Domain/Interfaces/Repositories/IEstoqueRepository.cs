using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;
using MechanicLtda.Domain.Interfaces.Repositories.Base;

namespace MechanicLtda.Domain.Interfaces.Repositories
{
    public interface IEstoqueRepository : IRepository<Estoque>
    {
        Task<IEnumerable<Estoque>> ObterPorTipoAsync(TipoEstoque tipo);
        Task<Estoque?> ObterPorNomeAsync(string nome);
    }
}