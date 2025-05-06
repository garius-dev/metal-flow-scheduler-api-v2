using AutoMapper;
using MetalFlowScheduler.Api.Application.Dtos;
using MetalFlowScheduler.Api.Application.Interfaces;
using MetalFlowScheduler.Api.Domain.Entities;
using MetalFlowScheduler.Api.Domain.Interfaces;
using MetalFlowScheduler.Api.Application.Exceptions; // Using custom exceptions
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore; // For DbUpdateException, if specific handling is needed beyond generic

namespace MetalFlowScheduler.Api.Application.Services
{
    /// <summary>
    /// Service for managing operations related to the Product entity.
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IOperationTypeRepository _operationTypeRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            IProductRepository productRepository,
            IOperationTypeRepository operationTypeRepository,
            IMapper mapper,
            ILogger<ProductService> logger)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _operationTypeRepository = operationTypeRepository ?? throw new ArgumentNullException(nameof(operationTypeRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ProductDto>> GetAllEnabledAsync()
        {
            var products = await _productRepository.GetAllEnabledAsync();
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }

        /// <inheritdoc/>
        public async Task<ProductDto> GetByIdAsync(int id)
        {
            var product = await _productRepository.GetByIdWithDetailsAsync(id);

            if (product == null || !product.Enabled)
            {
                _logger.LogWarning("Product with ID {ProductId} not found or not enabled.", id);
                throw new NotFoundException(nameof(Product), id);
            }
            return _mapper.Map<ProductDto>(product);
        }

        /// <inheritdoc/>
        public async Task<ProductDto> CreateAsync(CreateProductDto createDto)
        {
            await ValidateOperationTypesAsync(createDto.OperationTypeIds);

            var existingProducts = await _productRepository.FindAsync(p => p.Name.ToLower() == createDto.Name.ToLower());
            var existingActive = existingProducts.FirstOrDefault(p => p.Enabled);
            var existingInactive = existingProducts.FirstOrDefault(p => !p.Enabled);

            if (existingActive != null)
            {
                _logger.LogWarning("Attempted to create Product with duplicate active name: {ProductName}", createDto.Name);
                throw new ConflictException($"A Product with the name '{createDto.Name}' already exists and is active.");
            }

            Product productToProcess;
            bool isReactivating = false;

            if (existingInactive != null)
            {
                _logger.LogInformation("Reactivating and updating inactive Product with name: {ProductName}, ID: {ProductId}", createDto.Name, existingInactive.ID);
                productToProcess = existingInactive;
                var existingDetails = await _productRepository.GetByIdWithDetailsAsync(productToProcess.ID); // Ensure routes are loaded
                if (existingDetails?.OperationRoutes != null)
                {
                    productToProcess.OperationRoutes.Clear(); // Clear existing routes on reactivation
                }
                _mapper.Map(createDto, productToProcess);
                productToProcess.Enabled = true;
                isReactivating = true;
            }
            else
            {
                productToProcess = _mapper.Map<Product>(createDto);
            }

            if (createDto.OperationTypeIds != null && createDto.OperationTypeIds.Any())
            {
                ManageOperationRoutes(productToProcess, createDto.OperationTypeIds);
            }

            try
            {
                if (isReactivating)
                {
                    await _productRepository.UpdateAsync(productToProcess);
                }
                else
                {
                    await _productRepository.AddAsync(productToProcess);
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while creating/reactivating Product: {ProductName}", createDto.Name);
                // Consider a more specific exception or rethrow if the middleware handles DbUpdateException appropriately
                throw new Exception("A database error occurred while saving the product.", ex);
            }

            // Fetch again to ensure all navigation properties are loaded for the DTO, especially if not fully mapped from productToProcess
            return await GetByIdAsync(productToProcess.ID);
        }

        /// <inheritdoc/>
        public async Task<ProductDto> UpdateAsync(int id, UpdateProductDto updateDto)
        {
            var product = await _productRepository.GetByIdWithDetailsAsync(id);

            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found for update.", id);
                throw new NotFoundException(nameof(Product), id);
            }

            if (!product.Enabled)
            {
                _logger.LogWarning("Attempted to update inactive Product with ID {ProductId}.", id);
                throw new ConflictException($"Cannot update an inactive Product (ID: {id}). Consider reactivating it first.");
            }

            await ValidateOperationTypesAsync(updateDto.OperationTypeIds);

            if (!string.Equals(product.Name, updateDto.Name, StringComparison.OrdinalIgnoreCase))
            {
                var conflictingProduct = (await _productRepository.FindAsync(p =>
                    p.Name.ToLower() == updateDto.Name.ToLower() && p.ID != id && p.Enabled))
                    .FirstOrDefault();
                if (conflictingProduct != null)
                {
                    _logger.LogWarning("Product update for ID {ProductId} resulted in a name conflict with Product ID {ConflictingProductId} for name {ProductName}", id, conflictingProduct.ID, updateDto.Name);
                    throw new ConflictException($"Another active Product with the name '{updateDto.Name}' already exists.");
                }
            }

            _mapper.Map(updateDto, product);
            ManageOperationRoutes(product, updateDto.OperationTypeIds);

            try
            {
                await _productRepository.UpdateAsync(product);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating Product ID {ProductId}.", id);
                throw new Exception("A database error occurred while updating the product.", ex);
            }

            return await GetByIdAsync(product.ID);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found for deletion.", id);
                throw new NotFoundException(nameof(Product), id, "Product not found for deletion.");
            }

            if (!product.Enabled)
            {
                _logger.LogInformation("Product with ID {ProductId} is already inactive.", id);
                return true;
            }

            // TODO: Add business logic validation before deletion (e.g., check for active production orders)
            // if (await _productionOrderRepository.AnyActiveOrdersForProductAsync(id))
            // {
            //    _logger.LogWarning("Attempted to delete Product ID {ProductId} with active production orders.", id);
            //    throw new ConflictException($"Product ID {id} cannot be disabled as it has active production orders.");
            // }

            try
            {
                await _productRepository.SoftRemoveAsync(product);
                _logger.LogInformation("Product with ID {ProductId} was successfully disabled.", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling Product ID {ProductId}", id);
                // Let the global handler manage this, or throw a specific internal server error exception
                throw new Exception($"An error occurred while disabling Product ID {id}.", ex);
            }
        }

        /// <summary>
        /// Validates if all OperationType IDs (if provided) exist and are active.
        /// Throws ValidationException if any are invalid.
        /// </summary>
        private async Task ValidateOperationTypesAsync(List<int>? operationTypeIds)
        {
            if (operationTypeIds == null || !operationTypeIds.Any()) return;

            var uniqueIds = operationTypeIds.Distinct().ToList();
            var operationTypes = await _operationTypeRepository.FindAsync(ot => uniqueIds.Contains(ot.ID)); // Fetch all, then filter by enabled

            var foundAndEnabledTypes = operationTypes.Where(ot => ot.Enabled).ToList();

            if (foundAndEnabledTypes.Count() != uniqueIds.Count)
            {
                var allFoundIds = operationTypes.Select(ot => ot.ID).ToList();
                var completelyMissingIds = uniqueIds.Except(allFoundIds).ToList();
                var foundButDisabledIds = allFoundIds.Except(foundAndEnabledTypes.Select(ot => ot.ID)).ToList();

                var errorMessages = new List<string>();
                if (completelyMissingIds.Any())
                {
                    errorMessages.Add($"OperationType IDs not found: {string.Join(", ", completelyMissingIds)}.");
                }
                if (foundButDisabledIds.Any())
                {
                    errorMessages.Add($"OperationType IDs are inactive: {string.Join(", ", foundButDisabledIds)}.");
                }
                _logger.LogWarning("Validation failed for OperationTypeIds: {ValidationErrors}", string.Join(" ", errorMessages));
                throw new ValidationException(new Dictionary<string, string[]> { { "OperationTypeIds", errorMessages.ToArray() } });
            }
        }

        /// <summary>
        /// Manages the collection of ProductOperationRoutes based on the IDs provided in the DTO.
        /// </summary>
        private void ManageOperationRoutes(Product product, List<int>? operationTypeIdsFromDto)
        {
            product.OperationRoutes ??= new List<ProductOperationRoute>();
            var existingRouteOpTypeIds = product.OperationRoutes.Select(r => r.OperationTypeID).ToList();
            var dtoOpTypeIds = operationTypeIdsFromDto?.Distinct().ToList() ?? new List<int>();

            var routesToRemove = product.OperationRoutes.Where(r => !dtoOpTypeIds.Contains(r.OperationTypeID)).ToList();
            foreach (var route in routesToRemove)
            {
                product.OperationRoutes.Remove(route);
            }

            var opTypeIdsToAdd = dtoOpTypeIds.Except(existingRouteOpTypeIds).ToList();
            int currentMaxOrder = product.OperationRoutes.Any() ? product.OperationRoutes.Max(r => r.Order) : 0;
            foreach (var opTypeId in opTypeIdsToAdd)
            {
                var newRoute = new ProductOperationRoute
                {
                    OperationTypeID = opTypeId,
                    Order = ++currentMaxOrder,
                    Version = 1, // Default version
                    EffectiveStartDate = DateTime.UtcNow,
                    EffectiveEndDate = null
                };
                product.OperationRoutes.Add(newRoute);
            }
        }
    }
}
