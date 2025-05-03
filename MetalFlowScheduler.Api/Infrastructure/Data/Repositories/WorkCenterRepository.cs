using MetalFlowScheduler.Api.Domain.Entities;
using MetalFlowScheduler.Api.Domain.Interfaces;
using MetalFlowScheduler.Api.Infrastructure.Data; // Para ApplicationDbContext
using Microsoft.EntityFrameworkCore; // Para Include e ToListAsync
using System.Collections.Generic; // Para IEnumerable
using System.Linq; // Para Where
using System.Threading.Tasks; // Para Task

namespace MetalFlowScheduler.Api.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Implementação do repositório específico para a entidade WorkCenter.
    /// </summary>
    public class WorkCenterRepository : Repository<WorkCenter>, IWorkCenterRepository
    {
        /// <summary>
        /// Construtor que passa o DbContext para a classe base.
        /// </summary>
        /// <param name="context">O contexto do banco de dados.</param>
        public WorkCenterRepository(ApplicationDbContext context) : base(context) { }

        /// <inheritdoc/>
        public async Task<WorkCenter?> GetByIdWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(wc => wc.Line) // Inclui a Linha associada
                .Include(wc => wc.OperationRoutes) // Inclui as rotas de operação associadas
                    .ThenInclude(opr => opr.OperationType) // Opcional: incluir detalhes da rota
                .FirstOrDefaultAsync(wc => wc.ID == id); // Busca pelo ID
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<WorkCenter>> GetAllEnabledWithDetailsAsync()
        {
            return await _dbSet
                .Where(wc => wc.Enabled) // Filtra apenas os ativos
                .Include(wc => wc.Line) // Inclui a Linha associada
                .ToListAsync();
        }

        // GetAllEnabledAsync é herdado da classe base Repository<WorkCenter>
    }
}
