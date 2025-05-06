// Arquivo: Domain/Interfaces/IOperationRepository.cs
using MetalFlowScheduler.Api.Domain.Entities;
using System.Collections.Generic; // Para IEnumerable
using System.Threading.Tasks; // Para Task

namespace MetalFlowScheduler.Api.Domain.Interfaces
{
    /// <summary>
    /// Interface específica para o repositório da entidade Operation.
    /// Herda as operações básicas de IRepository<Operation>.
    /// </summary>
    public interface IOperationRepository : IRepository<Operation>
    {
        // Métodos específicos para Operation podem ser adicionados aqui, se necessário.

        /// <summary>
        /// Obtém uma operação pelo seu ID, incluindo detalhes relacionados (OperationType, WorkCenter).
        /// </summary>
        /// <param name="id">O ID da operação.</param>
        /// <returns>A entidade Operation com detalhes, ou null se não encontrada.</returns>
        Task<Operation?> GetByIdWithDetailsAsync(int id);

        /// <summary>
        /// Obtém todas as operações ativas, incluindo detalhes relacionados (OperationType, WorkCenter).
        /// </summary>
        /// <returns>Uma coleção de entidades Operation ativas com detalhes.</returns>
        Task<IEnumerable<Operation>> GetAllEnabledWithDetailsAsync();
    }
}
