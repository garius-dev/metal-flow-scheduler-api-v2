using MetalFlowScheduler.Api.Application.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MetalFlowScheduler.Api.Application.Interfaces
{
    /// <summary>
    /// Interface para o serviço de gerenciamento de Produtos.
    /// </summary>
    public interface IProductService
    {
        /// <summary>
        /// Obtém todos os produtos ativos.
        /// </summary>
        Task<IEnumerable<ProductDto>> GetAllEnabledAsync();

        /// <summary>
        /// Obtém um produto pelo seu ID. Retorna null se não encontrado ou inativo.
        /// </summary>
        Task<ProductDto?> GetByIdAsync(int id);

        /// <summary>
        /// Cria um novo produto, incluindo opcionalmente suas rotas de operação.
        /// </summary>
        Task<ProductDto> CreateAsync(CreateProductDto createDto);

        /// <summary>
        /// Atualiza um produto existente, gerenciando opcionalmente suas rotas de operação. Retorna null se não encontrado.
        /// </summary>
        Task<ProductDto?> UpdateAsync(int id, UpdateProductDto updateDto);

        /// <summary>
        /// Desabilita (soft delete) um produto. Retorna true se bem-sucedido, false caso contrário.
        /// </summary>
        Task<bool> DeleteAsync(int id);
    }
}
