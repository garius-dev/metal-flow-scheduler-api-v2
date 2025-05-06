// Arquivo: Infrastructure/Data/Repositories/OperationRepository.cs
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
    /// Implementação do repositório específico para a entidade Operation.
    /// </summary>
    public class OperationRepository : Repository<Operation>, IOperationRepository
    {
        /// <summary>
        /// Construtor que passa o DbContext para a classe base.
        /// </summary>
        /// <param name="context">O contexto do banco de dados.</param>
        public OperationRepository(ApplicationDbContext context) : base(context) { }

        /// <inheritdoc/>
        public async Task<Operation?> GetByIdWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(o => o.OperationType) // Inclui o Tipo de Operação associado
                .Include(o => o.WorkCenter) // Inclui o Centro de Trabalho associado
                .FirstOrDefaultAsync(o => o.ID == id); // Busca pelo ID
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Operation>> GetAllEnabledWithDetailsAsync()
        {
            return await _dbSet
                .Where(o => o.Enabled) // Filtra apenas os ativos
                .Include(o => o.OperationType) // Inclui o Tipo de Operação associado
                .Include(o => o.WorkCenter) // Inclui o Centro de Trabalho associado
                .ToListAsync();
        }

        // Métodos herdados da classe base Repository<Operation>
    }
}
