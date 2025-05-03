using MetalFlowScheduler.Api.Domain.Entities;

namespace MetalFlowScheduler.Api.Domain.Interfaces
{
    /// <summary>
    /// Interface específica para o repositório da entidade OperationType.
    /// Herda as operações básicas de IRepository<OperationType>.
    /// </summary>
    public interface IOperationTypeRepository : IRepository<OperationType>
    {
        // Métodos específicos para OperationType podem ser adicionados aqui, se necessário.
    }
}
