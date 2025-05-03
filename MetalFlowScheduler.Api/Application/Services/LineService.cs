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
    /// Serviço para gerenciar operações relacionadas à entidade Line.
    /// </summary>
    public class LineService : ILineService
    {
        private readonly ILineRepository _lineRepository;
        private readonly IWorkCenterRepository _workCenterRepository;
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<LineService> _logger;

        public LineService(
            ILineRepository lineRepository,
            IWorkCenterRepository workCenterRepository,
            IProductRepository productRepository,
            IMapper mapper,
            ILogger<LineService> logger)
        {
            _lineRepository = lineRepository ?? throw new ArgumentNullException(nameof(lineRepository));
            _workCenterRepository = workCenterRepository ?? throw new ArgumentNullException(nameof(workCenterRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<LineDto>> GetAllEnabledAsync()
        {
            var lines = await _lineRepository.GetAllEnabledAsync();
            return _mapper.Map<IEnumerable<LineDto>>(lines);
        }

        /// <inheritdoc/>
        public async Task<LineDto?> GetByIdAsync(int id)
        {
            var line = await _lineRepository.GetByIdWithDetailsAsync(id); // Carrega detalhes para consistência interna

            if (line == null || !line.Enabled)
            {
                return null;
            }
            return _mapper.Map<LineDto>(line);
        }

        /// <inheritdoc/>
        public async Task<LineDto> CreateAsync(CreateLineDto createDto)
        {
            await ValidateWorkCentersAsync(createDto.WorkCenterIds);
            await ValidateProductsAsync(createDto.ProductIds);

            // R01 & C11: Verificar nome existente
            var existingLines = await _lineRepository.FindAsync(l => l.Name.ToLower() == createDto.Name.ToLower());
            var existingActive = existingLines.FirstOrDefault(l => l.Enabled);
            var existingInactive = existingLines.FirstOrDefault(l => !l.Enabled);

            if (existingActive != null)
            {
                throw new Exception($"Já existe uma Linha ativa com o nome '{createDto.Name}'."); // TODO: Use ValidationException
            }

            Line lineToProcess;
            bool isReactivating = false;

            if (existingInactive != null)
            {
                // C11: Reativar e atualizar
                lineToProcess = existingInactive;
                var existingDetails = await _lineRepository.GetByIdWithDetailsAsync(lineToProcess.ID);
                if (existingDetails != null)
                {
                    // Limpa coleções existentes ao reativar (regra assumida)
                    lineToProcess.WorkCenterRoutes.Clear();
                    lineToProcess.AvailableProducts.Clear();
                }
                _mapper.Map(createDto, lineToProcess);
                lineToProcess.Enabled = true;
                isReactivating = true;
            }
            else
            {
                // Criar nova
                lineToProcess = _mapper.Map<Line>(createDto);
            }

            // C07: Criar LineWorkCenterRoute
            int routeOrder = 1;
            foreach (var wcId in createDto.WorkCenterIds.Distinct())
            {
                // Usando valores padrão para Version, TransportTime, etc. Ajustar se DTO for mais complexo.
                var newRoute = new LineWorkCenterRoute
                {
                    WorkCenterID = wcId,
                    Order = routeOrder++,
                    Version = 1,
                    TransportTimeInMinutes = 5, // Padrão - AJUSTAR
                    EffectiveStartDate = DateTime.UtcNow,
                    EffectiveEndDate = null,
                };
                lineToProcess.WorkCenterRoutes.Add(newRoute);
            }

            // C07: Criar ProductAvailablePerLine (se ProductIds fornecido)
            if (createDto.ProductIds != null && createDto.ProductIds.Any())
            {
                foreach (var prodId in createDto.ProductIds.Distinct())
                {
                    var newAvailability = new ProductAvailablePerLine { ProductID = prodId };
                    lineToProcess.AvailableProducts.Add(newAvailability);
                }
            }

            if (isReactivating)
            {
                await _lineRepository.UpdateAsync(lineToProcess);
            }
            else
            {
                await _lineRepository.AddAsync(lineToProcess);
            }

            return _mapper.Map<LineDto>(lineToProcess);
        }

        /// <inheritdoc/>
        public async Task<LineDto?> UpdateAsync(int id, UpdateLineDto updateDto)
        {
            var line = await _lineRepository.GetByIdWithDetailsAsync(id); // Carrega detalhes para C09

            if (line == null) return null;

            if (!line.Enabled)
            {
                throw new Exception($"Não é possível atualizar uma Linha inativa (ID: {id})."); // TODO: Use ValidationException
            }

            await ValidateWorkCentersAsync(updateDto.WorkCenterIds);
            await ValidateProductsAsync(updateDto.ProductIds);

            // R01: Verificar conflito de nome
            if (!string.Equals(line.Name, updateDto.Name, StringComparison.OrdinalIgnoreCase))
            {
                var conflictingLine = (await _lineRepository.FindAsync(l =>
                    l.Name.ToLower() == updateDto.Name.ToLower() && l.ID != id && l.Enabled))
                    .FirstOrDefault();
                if (conflictingLine != null)
                {
                    throw new Exception($"Já existe outra Linha ativa com o nome '{updateDto.Name}'."); // TODO: Use ValidationException
                }
            }

            _mapper.Map(updateDto, line);

            // C09: Gerenciamento inteligente das rotas
            ManageWorkCenterRoutes(line, updateDto.WorkCenterIds);

            // C09: Gerenciamento inteligente dos produtos disponíveis
            ManageAvailableProducts(line, updateDto.ProductIds);

            try
            {
                await _lineRepository.UpdateAsync(line);
                return _mapper.Map<LineDto>(line);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro ao atualizar linha ID {LineId} no banco de dados.", id);
                throw; // Re-lança para tratamento global
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(int id)
        {
            var line = await _lineRepository.GetByIdAsync(id);
            if (line == null) return false;
            if (!line.Enabled) return true; // Já inativa

            // TODO: Adicionar validações de negócio antes de deletar

            try
            {
                await _lineRepository.SoftRemoveAsync(line);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desabilitar linha ID {LineId}", id);
                return false;
            }
        }

        // --- Métodos Auxiliares ---

        /// <summary>
        /// Valida se todos os WorkCenter IDs existem e estão ativos.
        /// </summary>
        private async Task<List<WorkCenter>> ValidateWorkCentersAsync(List<int> workCenterIds)
        {
            if (workCenterIds == null || !workCenterIds.Any())
            {
                throw new Exception("A lista de IDs de Centro de Trabalho não pode ser vazia."); // TODO: Use ValidationException
            }
            var uniqueIds = workCenterIds.Distinct().ToList();
            var workCenters = await _workCenterRepository.FindAsync(wc => uniqueIds.Contains(wc.ID) && wc.Enabled);
            if (workCenters.Count() != uniqueIds.Count)
            {
                var missingIds = uniqueIds.Except(workCenters.Select(wc => wc.ID));
                throw new Exception($"Um ou mais IDs de Centro de Trabalho são inválidos ou inativos: {string.Join(", ", missingIds)}"); // TODO: Use ValidationException
            }
            return workCenters.ToList();
        }

        /// <summary>
        /// Valida se todos os Product IDs (se fornecidos) existem e estão ativos.
        /// </summary>
        private async Task<List<Product>?> ValidateProductsAsync(List<int>? productIds)
        {
            if (productIds == null || !productIds.Any()) return null;

            var uniqueIds = productIds.Distinct().ToList();
            var products = await _productRepository.FindAsync(p => uniqueIds.Contains(p.ID) && p.Enabled);
            if (products.Count() != uniqueIds.Count)
            {
                var missingIds = uniqueIds.Except(products.Select(p => p.ID));
                throw new Exception($"Um ou mais IDs de Produto são inválidos ou inativos: {string.Join(", ", missingIds)}"); // TODO: Use ValidationException
            }
            return products.ToList();
        }

        /// <summary>
        /// Gerencia a coleção de LineWorkCenterRoutes com base nos IDs fornecidos no DTO (C09).
        /// </summary>
        private void ManageWorkCenterRoutes(Line line, List<int> workCenterIdsFromDto)
        {
            line.WorkCenterRoutes ??= new List<LineWorkCenterRoute>();
            var existingRouteWcIds = line.WorkCenterRoutes.Select(r => r.WorkCenterID).ToList();
            var dtoWcIds = workCenterIdsFromDto.Distinct().ToList();

            // Rotas para remover
            var routesToRemove = line.WorkCenterRoutes.Where(r => !dtoWcIds.Contains(r.WorkCenterID)).ToList();
            foreach (var route in routesToRemove) line.WorkCenterRoutes.Remove(route);

            // IDs de WorkCenters para adicionar
            var wcIdsToAdd = dtoWcIds.Except(existingRouteWcIds).ToList();
            int currentMaxOrder = line.WorkCenterRoutes.Any() ? line.WorkCenterRoutes.Max(r => r.Order) : 0;
            foreach (var wcId in wcIdsToAdd)
            {
                // Usando valores padrão para Version, TransportTime, etc. Ajustar se DTO for mais complexo.
                var newRoute = new LineWorkCenterRoute
                {
                    WorkCenterID = wcId,
                    Order = ++currentMaxOrder,
                    Version = 1,
                    TransportTimeInMinutes = 5, // Padrão - AJUSTAR
                    EffectiveStartDate = DateTime.UtcNow,
                    EffectiveEndDate = null
                };
                line.WorkCenterRoutes.Add(newRoute);
            }
        }

        /// <summary>
        /// Gerencia a coleção de ProductAvailablePerLine com base nos IDs fornecidos no DTO (C09).
        /// </summary>
        private void ManageAvailableProducts(Line line, List<int>? productIdsFromDto)
        {
            line.AvailableProducts ??= new List<ProductAvailablePerLine>();
            var existingAvailableProdIds = line.AvailableProducts.Select(ap => ap.ProductID).ToList();
            var dtoProdIds = productIdsFromDto?.Distinct().ToList() ?? new List<int>();

            // Disponibilidades para remover
            var availabilityToRemove = line.AvailableProducts.Where(ap => !dtoProdIds.Contains(ap.ProductID)).ToList();
            foreach (var availability in availabilityToRemove) line.AvailableProducts.Remove(availability);

            // IDs de Produtos para adicionar
            var prodIdsToAdd = dtoProdIds.Except(existingAvailableProdIds).ToList();
            foreach (var prodId in prodIdsToAdd)
            {
                var newAvailability = new ProductAvailablePerLine { ProductID = prodId };
                line.AvailableProducts.Add(newAvailability);
            }
        }
    }
}
