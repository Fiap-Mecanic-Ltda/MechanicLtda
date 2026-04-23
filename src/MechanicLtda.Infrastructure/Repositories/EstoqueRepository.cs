using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Enums;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Infrastructure.Repositories
{
    public class EstoqueRepository : Repository<Estoque>, IEstoqueRepository
    {
        public EstoqueRepository(BancoAPIContext context) : base(context) { }

        public override async Task<Estoque?> ObterPorIdAsync(string id)
        {
            return await _dbSet
                .FirstOrDefaultAsync(e => e.Id == int.Parse(id));
        }

        public async Task<IEnumerable<Estoque>> ObterPorTipoAsync(TipoEstoque tipo)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(e => e.Tipo == tipo)
                .ToListAsync();
        }

        public async Task<Estoque?> ObterPorNomeAsync(string nome)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Nome.ToLower() == nome.ToLower());
        }
    }
}