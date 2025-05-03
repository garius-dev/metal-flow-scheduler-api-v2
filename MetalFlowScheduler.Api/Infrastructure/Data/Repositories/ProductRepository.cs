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
    /// Implementação do repositório específico para a entidade Product.
    /// </summary>
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        /// <summary>
        /// Construtor que passa o DbContext para a classe base.
        /// </summary>
        /// <param name="context">O contexto do banco de dados.</param>
        public ProductRepository(ApplicationDbContext context) : base(context) { }

        /// <inheritdoc/>
        public async Task<Product?> GetByIdWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(p => p.OperationRoutes) // Inclui as rotas de operação
                    .ThenInclude(opr => opr.OperationType) // Opcional: incluir detalhes do tipo de operação na rota
                .FirstOrDefaultAsync(p => p.ID == id); // Busca pelo ID
        }

        // GetAllEnabledAsync é herdado da classe base Repository<Product>.
    }
}
