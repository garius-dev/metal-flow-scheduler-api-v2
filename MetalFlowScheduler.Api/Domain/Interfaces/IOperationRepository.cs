using MetalFlowScheduler.Api.Domain.Entities;

namespace MetalFlowScheduler.Api.Domain.Interfaces
{
    /// <summary>
    /// Interface específica para o repositório da entidade Operation.
    /// Herda as operações básicas de IRepository<Operation>.
    /// </summary>
    public interface IOperationRepository : IRepository<Operation>
    {
        // Métodos específicos para Operation podem ser adicionados aqui, se necessário.
    }
}
