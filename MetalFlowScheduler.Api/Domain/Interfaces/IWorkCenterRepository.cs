using MetalFlowScheduler.Api.Domain.Entities;
using System.Collections.Generic; // Para IEnumerable
using System.Threading.Tasks; // Para Task

namespace MetalFlowScheduler.Api.Domain.Interfaces
{
    /// <summary>
    /// Interface específica para o repositório da entidade WorkCenter.
    /// Herda as operações básicas de IRepository<WorkCenter>.
    /// </summary>
    public interface IWorkCenterRepository : IRepository<WorkCenter>
    {
        /// <summary>
        /// Obtém um centro de trabalho pelo seu ID, incluindo detalhes relacionados (Linha, Rotas de Operação).
        /// </summary>
        /// <param name="id">O ID do centro de trabalho.</param>
        /// <returns>A entidade WorkCenter com detalhes, ou null se não encontrado.</returns>
        Task<WorkCenter?> GetByIdWithDetailsAsync(int id);

        /// <summary>
        /// Obtém todos os centros de trabalho ativos, incluindo detalhes relacionados (Linha).
        /// </summary>
        /// <returns>Uma coleção de entidades WorkCenter ativas com detalhes.</returns>
        Task<IEnumerable<WorkCenter>> GetAllEnabledWithDetailsAsync();
    }
}
