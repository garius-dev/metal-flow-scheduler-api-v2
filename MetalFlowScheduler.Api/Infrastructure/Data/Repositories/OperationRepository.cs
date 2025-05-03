using MetalFlowScheduler.Api.Domain.Entities;
using MetalFlowScheduler.Api.Domain.Interfaces;
using MetalFlowScheduler.Api.Infrastructure.Data; // Para ApplicationDbContext

namespace MetalFlowScheduler.Api.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Implementação do repositório específico para a entidade Operation.
    /// </summary>
    public class OperationRepository : Repository<Operation>, IOperationRepository
    {
        /// <summary>
        /// Construtor que passa o DbContext para a classe base.
        /// </summary>
        /// <param name="context">O contexto do banco de dados.</param>
        public OperationRepository(ApplicationDbContext context) : base(context) { }

        // Métodos específicos para Operation podem ser adicionados aqui, se necessário.
        // Exemplo: Implementação de um método para buscar operações com detalhes
        // public async Task<IEnumerable<Operation>> GetAllEnabledWithDetailsAsync() {
        //     return await _dbSet.Where(o => o.Enabled)
        //                        .Include(o => o.OperationType)
        //                        .Include(o => o.WorkCenter)
        //                        .ToListAsync();
        // }
    }
}
