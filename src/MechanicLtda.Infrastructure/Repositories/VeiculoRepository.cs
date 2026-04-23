using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Infrastructure.Repositories
{
    public class VeiculoRepository : Repository<Veiculo>, IVeiculoRepository
    {
        public VeiculoRepository(BancoAPIContext context) : base(context) { }

        public async Task<bool> PlacaExisteAsync(string placa)
        {
            return await _dbSet.AnyAsync(v => v.Placa == placa.ToUpper());
        }

        public async Task<IEnumerable<Veiculo>> ObterPorClienteIdAsync(int clienteId)
        {
            return await _dbSet.AsNoTracking()
                               .Where(v => v.ClienteId == clienteId)
                               .ToListAsync();
        }
    }
}