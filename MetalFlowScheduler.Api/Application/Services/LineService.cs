// Arquivo: Application/Services/LineService.cs
using AutoMapper;
using MetalFlowScheduler.Api.Application.Dtos;
using MetalFlowScheduler.Api.Application.Interfaces;
using MetalFlowScheduler.Api.Domain.Entities;
using MetalFlowScheduler.Api.Domain.Interfaces;
using MetalFlowScheduler.Api.Application.Exceptions; // Importar as exceções customizadas
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
                // Lança NotFoundException se não encontrado ou inativo
                throw new NotFoundException($"Linha com ID {id} não encontrada (ou inativa).");
            }
            return _mapper.Map<LineDto>(line);
        }

        /// <inheritdoc/>
        public async Task<LineDto> CreateAsync(CreateLineDto createDto)
        {
            // Validações lançarão ValidationException ou NotFoundException se falharem
            await ValidateWorkCentersAsync(createDto.WorkCenterIds);
            await ValidateProductsAsync(createDto.ProductIds);

            // R01 & C11: Verificar nome existente
            var existingLines = await _lineRepository.FindAsync(l => l.Name.ToLower() == createDto.Name.ToLower());
            var existingActive = existingLines.FirstOrDefault(l => l.Enabled);
            var existingInactive = existingLines.FirstOrDefault(l => !l.Enabled);

            if (existingActive != null)
            {
                // Viola R01 (nome único ativo) - Lança ConflictException
                throw new ConflictException($"Já existe uma Linha ativa com o nome '{createDto.Name}'.");
            }

            Line lineToProcess;
            bool isReactivating = false;

            if (existingInactive != null)
            {
                // C11: Reativar e atualizar item inativo existente
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

            // Busca novamente para retornar o DTO com detalhes carregados (opcional, mas boa prática)
            return await GetByIdAsync(lineToProcess.ID) ??
                   throw new Exception("Falha inesperada ao buscar a linha recém-criada/atualizada."); // Should not happen if Add/Update succeeded
        }

        /// <inheritdoc/>
        public async Task<LineDto?> UpdateAsync(int id, UpdateLineDto updateDto)
        {
            var line = await _lineRepository.GetByIdWithDetailsAsync(id); // Carrega detalhes para C09

            if (line == null)
            {
                // Lança NotFoundException se não encontrado
                throw new NotFoundException($"Linha com ID {id} não encontrada.");
            }

            if (!line.Enabled)
            {
                // Regra de negócio: não permitir atualizar inativos diretamente - Lança ConflictException
                throw new ConflictException($"Não é possível atualizar uma Linha inativa (ID: {id}). Considere reativá-la primeiro.");
            }

            // Validações lançarão ValidationException ou NotFoundException se falharem
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
                    // Lança ConflictException se houver conflito de nome
                    throw new ConflictException($"Já existe outra Linha ativa com o nome '{updateDto.Name}'.");
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
                // Busca novamente para retornar o DTO com detalhes carregados
                return await GetByIdAsync(line.ID);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro ao atualizar linha ID {LineId} no banco de dados.", id);
                // Re-lança para tratamento global no middleware
                throw new Exception($"Falha ao salvar as alterações para a Linha com ID {id}.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(int id)
        {
            var line = await _lineRepository.GetByIdAsync(id);
            if (line == null)
            {
                // Lança NotFoundException se não encontrado
                throw new NotFoundException($"Linha com ID {id} não encontrada para exclusão.");
            }

            if (!line.Enabled) return true; // Já inativa, considera sucesso sem erro

            // TODO: Adicionar validações de negócio antes de deletar (ex: verificar dependências ativas)
            // Exemplo: Verificar se há WorkCenters ativos associados a esta linha
            // var hasActiveWorkCenters = await _workCenterRepository.FindAsync(wc => wc.LineID == id && wc.Enabled);
            // if (hasActiveWorkCenters.Any())
            // {
            //     throw new ConflictException($"Não é possível desabilitar a Linha ID {id} pois existem Centros de Trabalho ativos associados a ela.");
            // }


            try
            {
                await _lineRepository.SoftRemoveAsync(line); // Soft delete
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desabilitar linha ID {LineId}", id);
                // Lança uma exceção genérica ou customizada se a falha for inesperada no repositório
                throw new Exception($"Falha ao desabilitar a Linha com ID {id}.", ex);
            }
        }

        // --- Métodos Auxiliares ---

        /// <summary>
        /// Valida se todos os WorkCenter IDs existem e estão ativos.
        /// Lança ValidationException ou NotFoundException se falhar.
        /// </summary>
        private async Task ValidateWorkCentersAsync(List<int> workCenterIds)
        {
            if (workCenterIds == null || !workCenterIds.Any())
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { nameof(CreateLineDto.WorkCenterIds), new[] { "É necessário fornecer os IDs dos centros de trabalho." } }
                });
            }
            var uniqueIds = workCenterIds.Distinct().ToList();
            var workCenters = await _workCenterRepository.FindAsync(wc => uniqueIds.Contains(wc.ID) && wc.Enabled);
            if (workCenters.Count() != uniqueIds.Count)
            {
                var missingIds = uniqueIds.Except(workCenters.Select(wc => wc.ID));
                // Lança ValidationException ou NotFoundException (dependendo da granularidade desejada)
                throw new ValidationException($"Um ou mais IDs de Centro de Trabalho são inválidos ou inativos: {string.Join(", ", missingIds)}");
            }
        }

        /// <summary>
        /// Valida se todos os Product IDs (se fornecidos) existem e estão ativos.
        /// Lança ValidationException ou NotFoundException se falhar.
        /// </summary>
        private async Task ValidateProductsAsync(List<int>? productIds)
        {
            if (productIds == null || !productIds.Any()) return; // Lista opcional vazia ou nula é válida

            var uniqueIds = productIds.Distinct().ToList();
            var products = await _productRepository.FindAsync(p => uniqueIds.Contains(p.ID) && p.Enabled);
            if (products.Count() != uniqueIds.Count)
            {
                var missingIds = uniqueIds.Except(products.Select(p => p.ID));
                // Lança ValidationException ou NotFoundException
                throw new ValidationException($"Um ou mais IDs de Produto são inválidos ou inativos: {string.Join(", ", missingIds)}");
            }
        }

        /// <summary>
        /// Gerencia a coleção de LineWorkCenterRoutes com base nos IDs fornecidos no DTO (C09).
        /// Assume que a Line entidade já foi carregada com WorkCenterRoutes incluídas.
        /// </summary>
        private void ManageWorkCenterRoutes(Line line, List<int> workCenterIdsFromDto)
        {
            line.WorkCenterRoutes ??= new List<LineWorkCenterRoute>();
            var existingRouteWcIds = line.WorkCenterRoutes.Select(r => r.WorkCenterID).ToList();
            var dtoWcIds = workCenterIdsFromDto.Distinct().ToList();

            // Rotas para remover: as que existem na entidade mas não estão no DTO
            var routesToRemove = line.WorkCenterRoutes.Where(r => !dtoWcIds.Contains(r.WorkCenterID)).ToList();
            foreach (var route in routesToRemove) line.WorkCenterRoutes.Remove(route);

            // IDs de WorkCenters para adicionar: os que estão no DTO mas não existem na entidade
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

            // Opcional: Atualizar a ordem das rotas existentes se a ordem for importante na lista do DTO
            // Isso exigiria que o DTO de atualização passasse a ordem desejada para cada WorkCenterId
        }

        /// <summary>
        /// Gerencia a coleção de ProductAvailablePerLine com base nos IDs fornecidos no DTO (C09).
        /// Assume que a Line entidade já foi carregada com AvailableProducts incluídos.
        /// </summary>
        private void ManageAvailableProducts(Line line, List<int>? productIdsFromDto)
        {
            line.AvailableProducts ??= new List<ProductAvailablePerLine>();
            var existingAvailableProdIds = line.AvailableProducts.Select(ap => ap.ProductID).ToList();
            var dtoProdIds = productIdsFromDto?.Distinct().ToList() ?? new List<int>();

            // Disponibilidades para remover: as que existem na entidade mas não estão no DTO
            var availabilityToRemove = line.AvailableProducts.Where(ap => !dtoProdIds.Contains(ap.ProductID)).ToList();
            foreach (var availability in availabilityToRemove) line.AvailableProducts.Remove(availability);

            // IDs de Produtos para adicionar: os que estão no DTO mas não existem na entidade
            var prodIdsToAdd = dtoProdIds.Except(existingAvailableProdIds).ToList();
            foreach (var prodId in prodIdsToAdd)
            {
                var newAvailability = new ProductAvailablePerLine { ProductID = prodId };
                line.AvailableProducts.Add(newAvailability);
            }
        }
    }
}
