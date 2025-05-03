using MetalFlowScheduler.Api.Domain.Entities;
using System.Collections.Generic; // Para IEnumerable
using System.Threading.Tasks; // Para Task

namespace MetalFlowScheduler.Api.Domain.Interfaces
{
    /// <summary>
    /// Interface específica para o repositório da entidade Product.
    /// Herda as operações básicas de IRepository<Product>.
    /// </summary>
    public interface IProductRepository : IRepository<Product>
    {
        /// <summary>
        /// Obtém um produto pelo seu ID, incluindo detalhes relacionados
        /// (Rotas de Operação).
        /// </summary>
        /// <param name="id">O ID do produto.</param>
        /// <returns>A entidade Product com detalhes, ou null se não encontrada.</returns>
        Task<Product?> GetByIdWithDetailsAsync(int id);

        /// <summary>
        /// Obtém todos os produtos ativos. Detalhes como rotas geralmente
        /// não são carregados aqui para performance.
        /// </summary>
        /// <returns>Uma coleção de entidades Product ativas.</returns>
        Task<IEnumerable<Product>> GetAllEnabledAsync(); // Herdado de IRepository, mas reafirmado para clareza
    }
}
