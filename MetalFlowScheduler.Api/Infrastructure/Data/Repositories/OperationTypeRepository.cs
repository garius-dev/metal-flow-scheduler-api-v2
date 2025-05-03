using MetalFlowScheduler.Api.Domain.Entities;
using MetalFlowScheduler.Api.Domain.Interfaces;
using MetalFlowScheduler.Api.Infrastructure.Data; // Para ApplicationDbContext

namespace MetalFlowScheduler.Api.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Implementação do repositório específico para a entidade OperationType.
    /// </summary>
    public class OperationTypeRepository : Repository<OperationType>, IOperationTypeRepository
    {
        /// <summary>
        /// Construtor que passa o DbContext para a classe base.
        /// </summary>
        /// <param name="context">O contexto do banco de dados.</param>
        public OperationTypeRepository(ApplicationDbContext context) : base(context) { }

        // Métodos específicos para OperationType podem ser adicionados aqui, se necessário.
    }
}
