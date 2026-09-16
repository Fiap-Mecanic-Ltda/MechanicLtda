using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace MechanicLtda.Infrastructure.Repositories
{
    public class ClienteRepository : Repository<Cliente>, IClienteRepository
    {
        public ClienteRepository(BancoAPIContext context) : base(context) { }

        public async Task<bool> EmailExisteAsync(string email)
        {
            return await _dbSet.AnyAsync(c => c.Email == email);
        }

        /// <summary>
        /// Consulta de leitura, usada para checar e-mail duplicado. Sem
        /// AsNoTracking, atualizar um cliente mantendo o próprio e-mail traz esta
        /// instância rastreada e o Update com outra instância do mesmo Id falha.
        /// Mesmo tratamento do <see cref="UsuarioRepository.ObterPorEmailAsync"/>.
        /// </summary>
        public async Task<Cliente?> ObterPorEmailAsync(string email)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(c => c.Email == email);
        }
    }
}
