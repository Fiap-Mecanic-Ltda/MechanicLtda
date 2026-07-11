using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Infrastructure.Repositories
{
    public class ServicoOficinaRepository : Repository<ServicoOficina>, IServicoOficinaRepository
    {
        public ServicoOficinaRepository(BancoAPIContext context) : base(context) { }

        public async Task<IEnumerable<ServicoOficina>> ObterAtivosAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(s => s.Ativo)
                .ToListAsync();
        }

        public async Task<ServicoOficina?> ObterPorNomeAsync(string nome)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Nome.ToLower() == nome.ToLower());
        }
    }
}
