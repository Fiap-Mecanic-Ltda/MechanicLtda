using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Infrastructure.Repositories
{
    public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
    {
        public UsuarioRepository(BancoAPIContext context) : base(context)
        {
        }

        public async Task<bool> EmailExisteAsync(string email)
        {
            return await _dbSet.AnyAsync(x => x.Email == email);
        }

        public async Task<Usuario?> ObterPorEmailAsync(string email)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email);
        }
    }
}
