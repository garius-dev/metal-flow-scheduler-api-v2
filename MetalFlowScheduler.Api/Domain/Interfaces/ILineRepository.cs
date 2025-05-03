using MetalFlowScheduler.Api.Domain.Entities;
using System.Collections.Generic; // Para IEnumerable
using System.Threading.Tasks; // Para Task

namespace MetalFlowScheduler.Api.Domain.Interfaces
{
    /// <summary>
    /// Interface específica para o repositório da entidade Line.
    /// Herda as operações básicas de IRepository<Line>.
    /// </summary>
    public interface ILineRepository : IRepository<Line>
    {
        /// <summary>
        /// Obtém uma linha pelo seu ID, incluindo detalhes relacionados
        /// (Rotas de WorkCenter e Produtos Disponíveis).
        /// </summary>
        /// <param name="id">O ID da linha.</param>
        /// <returns>A entidade Line com detalhes, ou null se não encontrada.</returns>
        Task<Line?> GetByIdWithDetailsAsync(int id);

        /// <summary>
        /// Obtém todas as linhas ativas. Os detalhes relacionados (rotas, produtos)
        /// geralmente não são carregados aqui para evitar sobrecarga na listagem geral.
        /// </summary>
        /// <returns>Uma coleção de entidades Line ativas.</returns>
        Task<IEnumerable<Line>> GetAllEnabledAsync(); // Herdado de IRepository, mas reafirmado para clareza
    }
}
