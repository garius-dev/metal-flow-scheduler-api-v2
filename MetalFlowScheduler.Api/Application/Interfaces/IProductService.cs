using MetalFlowScheduler.Api.Application.Dtos;
using MetalFlowScheduler.Api.Application.Exceptions; // Required for exception documentation
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MetalFlowScheduler.Api.Application.Interfaces
{
    /// <summary>
    /// Interface for the service of managing Products.
    /// </summary>
    public interface IProductService
    {
        /// <summary>
        /// Gets all enabled products.
        /// </summary>
        /// <returns>A collection of enabled product DTOs.</returns>
        Task<IEnumerable<ProductDto>> GetAllEnabledAsync();

        /// <summary>
        /// Gets a product by its ID.
        /// </summary>
        /// <param name="id">The ID of the product.</param>
        /// <returns>The product DTO if found and enabled.</returns>
        /// <exception cref="NotFoundException">Thrown if the product is not found or is not enabled.</exception>
        Task<ProductDto> GetByIdAsync(int id);

        /// <summary>
        /// Creates a new product, optionally including its operation routes.
        /// </summary>
        /// <param name="createDto">The data to create the new product.</param>
        /// <returns>The created product DTO.</returns>
        /// <exception cref="ConflictException">Thrown if a product with the same name already exists and is active.</exception>
        /// <exception cref="ValidationException">Thrown if related entities (e.g., OperationTypes) are invalid or not found.</exception>
        Task<ProductDto> CreateAsync(CreateProductDto createDto);

        /// <summary>
        /// Updates an existing product, optionally managing its operation routes.
        /// </summary>
        /// <param name="id">The ID of the product to update.</param>
        /// <param name="updateDto">The new data for the product.</param>
        /// <returns>The updated product DTO.</returns>
        /// <exception cref="NotFoundException">Thrown if the product is not found.</exception>
        /// <exception cref="ConflictException">Thrown if the product is inactive or if a product with the same name already exists and is active.</exception>
        /// <exception cref="ValidationException">Thrown if related entities (e.g., OperationTypes) are invalid or not found.</exception>
        Task<ProductDto> UpdateAsync(int id, UpdateProductDto updateDto);

        /// <summary>
        /// Disables (soft delete) a product.
        /// </summary>
        /// <param name="id">The ID of the product to disable.</param>
        /// <returns>True if the product was successfully disabled or was already inactive.</returns>
        /// <exception cref="NotFoundException">Thrown if the product is not found.</exception>
        /// <exception cref="ConflictException">Thrown if there are business rule violations preventing deletion (e.g., active dependencies).</exception>
        Task<bool> DeleteAsync(int id);
    }
}
