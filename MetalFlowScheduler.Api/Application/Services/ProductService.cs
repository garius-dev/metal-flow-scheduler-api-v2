using AutoMapper;
using MetalFlowScheduler.Api.Application.Dtos;
using MetalFlowScheduler.Api.Application.Interfaces;
using MetalFlowScheduler.Api.Domain.Entities;
using MetalFlowScheduler.Api.Domain.Interfaces;
// using MetalFlowScheduler.Api.Application.Exceptions; // Para exceções customizadas (opcional)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore; // Para DbUpdateException

namespace MetalFlowScheduler.Api.Application.Services
{
    /// <summary>
    /// Serviço para gerenciar operações relacionadas à entidade Product.
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
        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            // Carrega detalhes para consistência interna, mesmo que DTO não os use diretamente
            var product = await _productRepository.GetByIdWithDetailsAsync(id);

            if (product == null || !product.Enabled)
            {
                return null;
            }
            return _mapper.Map<ProductDto>(product);
        }

        /// <inheritdoc/>
        public async Task<ProductDto> CreateAsync(CreateProductDto createDto)
        {
            await ValidateOperationTypesAsync(createDto.OperationTypeIds);

            // R01 & C11: Verificar nome existente
            var existingProducts = await _productRepository.FindAsync(p => p.Name.ToLower() == createDto.Name.ToLower());
            var existingActive = existingProducts.FirstOrDefault(p => p.Enabled);
            var existingInactive = existingProducts.FirstOrDefault(p => !p.Enabled);

            if (existingActive != null)
            {
                throw new Exception($"Já existe um Produto ativo com o nome '{createDto.Name}'."); // TODO: Use ValidationException
            }

            Product productToProcess;
            bool isReactivating = false;

            if (existingInactive != null)
            {
                // C11: Reativar e atualizar
                productToProcess = existingInactive;
                var existingDetails = await _productRepository.GetByIdWithDetailsAsync(productToProcess.ID);
                if (existingDetails?.OperationRoutes != null)
                {
                    // Limpa rotas existentes ao reativar (regra assumida)
                    productToProcess.OperationRoutes.Clear();
                }
                _mapper.Map(createDto, productToProcess);
                productToProcess.Enabled = true;
                isReactivating = true;
            }
            else
            {
                // Criar novo
                productToProcess = _mapper.Map<Product>(createDto);
            }

            // C07: Criar ProductOperationRoute (se OperationTypeIds fornecido)
            if (createDto.OperationTypeIds != null && createDto.OperationTypeIds.Any())
            {
                ManageOperationRoutes(productToProcess, createDto.OperationTypeIds); // Usa método auxiliar
            }

            if (isReactivating)
            {
                await _productRepository.UpdateAsync(productToProcess);
            }
            else
            {
                await _productRepository.AddAsync(productToProcess);
            }

            return _mapper.Map<ProductDto>(productToProcess);
        }

        /// <inheritdoc/>
        public async Task<ProductDto?> UpdateAsync(int id, UpdateProductDto updateDto)
        {
            var product = await _productRepository.GetByIdWithDetailsAsync(id); // Carrega detalhes para C09

            if (product == null) return null;

            if (!product.Enabled)
            {
                throw new Exception($"Não é possível atualizar um Produto inativo (ID: {id})."); // TODO: Use ValidationException
            }

            await ValidateOperationTypesAsync(updateDto.OperationTypeIds);

            // R01: Verificar conflito de nome
            if (!string.Equals(product.Name, updateDto.Name, StringComparison.OrdinalIgnoreCase))
            {
                var conflictingProduct = (await _productRepository.FindAsync(p =>
                    p.Name.ToLower() == updateDto.Name.ToLower() && p.ID != id && p.Enabled))
                    .FirstOrDefault();
                if (conflictingProduct != null)
                {
                    throw new Exception($"Já existe outro Produto ativo com o nome '{updateDto.Name}'."); // TODO: Use ValidationException
                }
            }

            _mapper.Map(updateDto, product);

            // C09: Gerenciamento inteligente das rotas
            ManageOperationRoutes(product, updateDto.OperationTypeIds);

            try
            {
                await _productRepository.UpdateAsync(product);
                return _mapper.Map<ProductDto>(product);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro ao atualizar produto ID {ProductId} no banco de dados.", id);
                throw; // Re-lança para tratamento global
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return false;
            if (!product.Enabled) return true; // Já inativa

            // TODO: Adicionar validações de negócio antes de deletar (ex: produto em uso?)

            try
            {
                await _productRepository.SoftRemoveAsync(product);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desabilitar produto ID {ProductId}", id);
                return false;
            }
        }

        // --- Métodos Auxiliares ---

        /// <summary>
        /// Valida se todos os OperationType IDs (se fornecidos) existem e estão ativos.
        /// </summary>
        private async Task<List<OperationType>?> ValidateOperationTypesAsync(List<int>? operationTypeIds)
        {
            if (operationTypeIds == null || !operationTypeIds.Any()) return null; // Lista opcional vazia ou nula é válida

            var uniqueIds = operationTypeIds.Distinct().ToList();
            var operationTypes = await _operationTypeRepository.FindAsync(ot => uniqueIds.Contains(ot.ID) && ot.Enabled);

            if (operationTypes.Count() != uniqueIds.Count)
            {
                var missingIds = uniqueIds.Except(operationTypes.Select(ot => ot.ID));
                throw new Exception($"Um ou mais IDs de Tipo de Operação são inválidos ou inativos: {string.Join(", ", missingIds)}"); // TODO: Use ValidationException
            }
            return operationTypes.ToList();
        }

        /// <summary>
        /// Gerencia a coleção de ProductOperationRoutes com base nos IDs fornecidos no DTO (C07/C09).
        /// </summary>
        private void ManageOperationRoutes(Product product, List<int>? operationTypeIdsFromDto)
        {
            product.OperationRoutes ??= new List<ProductOperationRoute>();
            var existingRouteOpTypeIds = product.OperationRoutes.Select(r => r.OperationTypeID).ToList();
            var dtoOpTypeIds = operationTypeIdsFromDto?.Distinct().ToList() ?? new List<int>();

            // Rotas para remover
            var routesToRemove = product.OperationRoutes.Where(r => !dtoOpTypeIds.Contains(r.OperationTypeID)).ToList();
            foreach (var route in routesToRemove) product.OperationRoutes.Remove(route);

            // IDs de Tipos de Operação para adicionar
            var opTypeIdsToAdd = dtoOpTypeIds.Except(existingRouteOpTypeIds).ToList();
            int currentMaxOrder = product.OperationRoutes.Any() ? product.OperationRoutes.Max(r => r.Order) : 0;
            foreach (var opTypeId in opTypeIdsToAdd)
            {
                // Usando valores padrão para Version, EffectiveStartDate, etc. Ajustar se DTO for mais complexo.
                var newRoute = new ProductOperationRoute
                {
                    OperationTypeID = opTypeId,
                    Order = ++currentMaxOrder,
                    Version = 1,
                    EffectiveStartDate = DateTime.UtcNow,
                    EffectiveEndDate = null
                };
                product.OperationRoutes.Add(newRoute);
            }
        }
    }
}
