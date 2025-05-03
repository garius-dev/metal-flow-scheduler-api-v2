using MetalFlowScheduler.Api.Domain.Entities;
using MetalFlowScheduler.Api.Domain.Interfaces;
using MetalFlowScheduler.Api.Infrastructure.Data; // Para ApplicationDbContext
using Microsoft.EntityFrameworkCore; // Para Include, ThenInclude e ToListAsync
using System.Collections.Generic; // Para IEnumerable
using System.Linq; // Para Where
using System.Threading.Tasks; // Para Task

namespace MetalFlowScheduler.Api.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Implementação do repositório específico para a entidade Line.
    /// </summary>
    public class LineRepository : Repository<Line>, ILineRepository
    {
        /// <summary>
        /// Construtor que passa o DbContext para a classe base.
        /// </summary>
        /// <param name="context">O contexto do banco de dados.</param>
        public LineRepository(ApplicationDbContext context) : base(context) { }

        /// <inheritdoc/>
        public async Task<Line?> GetByIdWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(l => l.WorkCenterRoutes)
                    .ThenInclude(lwr => lwr.WorkCenter) // Opcional: incluir detalhes do WorkCenter na rota
                .Include(l => l.AvailableProducts)
                    .ThenInclude(ap => ap.Product) // Opcional: incluir detalhes do Produto disponível
                .FirstOrDefaultAsync(l => l.ID == id);
        }

        // GetAllEnabledAsync é herdado da classe base Repository<Line>
    }
}
