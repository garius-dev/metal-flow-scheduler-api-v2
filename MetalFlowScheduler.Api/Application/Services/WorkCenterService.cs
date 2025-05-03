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
    /// Serviço para gerenciar operações relacionadas à entidade WorkCenter.
    /// </summary>
    public class WorkCenterService : IWorkCenterService
    {
        private readonly IWorkCenterRepository _workCenterRepository;
        private readonly ILineRepository _lineRepository;
        private readonly IOperationTypeRepository _operationTypeRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<WorkCenterService> _logger;

        public WorkCenterService(
            IWorkCenterRepository workCenterRepository,
            ILineRepository lineRepository,
            IOperationTypeRepository operationTypeRepository,
            IMapper mapper,
            ILogger<WorkCenterService> logger)
        {
            _workCenterRepository = workCenterRepository ?? throw new ArgumentNullException(nameof(workCenterRepository));
            _lineRepository = lineRepository ?? throw new ArgumentNullException(nameof(lineRepository));
            _operationTypeRepository = operationTypeRepository ?? throw new ArgumentNullException(nameof(operationTypeRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<WorkCenterDto>> GetAllEnabledAsync()
        {
            // Usa método do repositório que inclui detalhes da linha para o DTO
            var workCenters = await _workCenterRepository.GetAllEnabledWithDetailsAsync();
            return _mapper.Map<IEnumerable<WorkCenterDto>>(workCenters);
        }

        /// <inheritdoc/>
        public async Task<WorkCenterDto?> GetByIdAsync(int id)
        {
            // Usa método do repositório que inclui detalhes
            var workCenter = await _workCenterRepository.GetByIdWithDetailsAsync(id);

            if (workCenter == null || !workCenter.Enabled)
            {
                return null;
            }
            return _mapper.Map<WorkCenterDto>(workCenter);
        }

        /// <inheritdoc/>
        public async Task<WorkCenterDto> CreateAsync(CreateWorkCenterDto createDto)
        {
            await ValidateLineAsync(createDto.LineId);
            await ValidateOperationTypesAsync(createDto.OperationTypeIds);

            // C11: Verificar nome existente (assumindo unicidade global)
            var existingWorkCenters = await _workCenterRepository.FindAsync(wc => wc.Name.ToLower() == createDto.Name.ToLower());
            var existingActive = existingWorkCenters.FirstOrDefault(wc => wc.Enabled);
            var existingInactive = existingWorkCenters.FirstOrDefault(wc => !wc.Enabled);

            if (existingActive != null)
            {
                throw new Exception($"Já existe um Centro de Trabalho ativo com o nome '{createDto.Name}'."); // TODO: Use ValidationException
            }

            WorkCenter workCenterToProcess;
            bool isReactivating = false;

            if (existingInactive != null)
            {
                // C11: Reativar e atualizar
                workCenterToProcess = existingInactive;
                var existingDetails = await _workCenterRepository.GetByIdWithDetailsAsync(workCenterToProcess.ID);
                if (existingDetails?.OperationRoutes != null)
                {
                    // Limpa rotas existentes ao reativar (regra assumida)
                    workCenterToProcess.OperationRoutes.Clear();
                }
                _mapper.Map(createDto, workCenterToProcess);
                workCenterToProcess.Enabled = true;
                isReactivating = true;
            }
            else
            {
                // Criar novo
                workCenterToProcess = _mapper.Map<WorkCenter>(createDto);
            }

            // C07: Adicionar WorkCenterOperationRoute
            int order = 1;
            foreach (var opTypeId in createDto.OperationTypeIds.Distinct())
            {
                // Usando valores padrão para Version, TransportTime, etc. Ajustar se DTO for mais complexo.
                var newRoute = new WorkCenterOperationRoute
                {
                    OperationTypeID = opTypeId,
                    Order = order++,
                    Version = 1,
                    TransportTimeInMinutes = 0, // Padrão - AJUSTAR
                    EffectiveStartDate = DateTime.UtcNow,
                    EffectiveEndDate = null,
                };
                workCenterToProcess.OperationRoutes.Add(newRoute);
            }

            if (isReactivating)
            {
                await _workCenterRepository.UpdateAsync(workCenterToProcess);
            }
            else
            {
                await _workCenterRepository.AddAsync(workCenterToProcess);
            }

            return await GetByIdAsync(workCenterToProcess.ID) ??
                   throw new Exception("Falha ao buscar o centro de trabalho recém-criado/atualizado.");
        }

        /// <inheritdoc/>
        public async Task<WorkCenterDto?> UpdateAsync(int id, UpdateWorkCenterDto updateDto)
        {
            var workCenter = await _workCenterRepository.GetByIdWithDetailsAsync(id); // Carrega rotas existentes para C09

            if (workCenter == null) return null;

            if (!workCenter.Enabled)
            {
                throw new Exception($"Não é possível atualizar um Centro de Trabalho inativo (ID: {id})."); // TODO: Use ValidationException
            }

            await ValidateLineAsync(updateDto.LineId);
            await ValidateOperationTypesAsync(updateDto.OperationTypeIds);

            // Verificar conflito de nome
            if (!string.Equals(workCenter.Name, updateDto.Name, StringComparison.OrdinalIgnoreCase))
            {
                var conflictingWorkCenter = (await _workCenterRepository.FindAsync(wc =>
                    wc.Name.ToLower() == updateDto.Name.ToLower() && wc.ID != id && wc.Enabled))
                    .FirstOrDefault();
                if (conflictingWorkCenter != null)
                {
                    throw new Exception($"Já existe outro Centro de Trabalho ativo com o nome '{updateDto.Name}'."); // TODO: Use ValidationException
                }
            }

            _mapper.Map(updateDto, workCenter); // Mapeia DTO -> Entidade

            // C09: Gerenciamento inteligente das rotas
            ManageOperationRoutes(workCenter, updateDto.OperationTypeIds);

            try
            {
                await _workCenterRepository.UpdateAsync(workCenter);
                return await GetByIdAsync(workCenter.ID);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro ao atualizar centro de trabalho ID {WorkCenterId} no banco de dados.", id);
                throw; // Re-lança para tratamento global
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(int id)
        {
            var workCenter = await _workCenterRepository.GetByIdAsync(id);
            if (workCenter == null) return false;
            if (!workCenter.Enabled) return true; // Já inativo

            // TODO: Adicionar validações de negócio antes de deletar

            try
            {
                await _workCenterRepository.SoftRemoveAsync(workCenter); // Soft delete
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desabilitar centro de trabalho ID {WorkCenterId}", id);
                return false;
            }
        }

        // --- Métodos Auxiliares ---

        /// <summary>
        /// Valida se a Linha existe e está ativa.
        /// </summary>
        private async Task ValidateLineAsync(int lineId)
        {
            var line = await _lineRepository.GetByIdAsync(lineId);
            if (line == null || !line.Enabled)
            {
                throw new Exception($"Linha com ID {lineId} inválida ou inativa."); // TODO: Use ValidationException
            }
        }

        /// <summary>
        /// Valida se todos os OperationType IDs existem e estão ativos.
        /// </summary>
        private async Task<List<OperationType>> ValidateOperationTypesAsync(List<int> operationTypeIds)
        {
            if (operationTypeIds == null || !operationTypeIds.Any())
            {
                throw new Exception("A lista de IDs de Tipo de Operação não pode ser vazia."); // TODO: Use ValidationException
            }
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
        /// Gerencia a coleção de WorkCenterOperationRoutes com base nos IDs fornecidos no DTO (C09).
        /// </summary>
        private void ManageOperationRoutes(WorkCenter workCenter, List<int> operationTypeIdsFromDto)
        {
            workCenter.OperationRoutes ??= new List<WorkCenterOperationRoute>();
            var existingRouteOpTypeIds = workCenter.OperationRoutes.Select(r => r.OperationTypeID).ToList();
            var dtoOpTypeIds = operationTypeIdsFromDto.Distinct().ToList();

            // Rotas para remover
            var routesToRemove = workCenter.OperationRoutes.Where(r => !dtoOpTypeIds.Contains(r.OperationTypeID)).ToList();
            foreach (var route in routesToRemove) workCenter.OperationRoutes.Remove(route);

            // IDs de Tipos de Operação para adicionar
            var opTypeIdsToAdd = dtoOpTypeIds.Except(existingRouteOpTypeIds).ToList();
            int currentMaxOrder = workCenter.OperationRoutes.Any() ? workCenter.OperationRoutes.Max(r => r.Order) : 0;
            foreach (var opTypeId in opTypeIdsToAdd)
            {
                // Usando valores padrão para Version, TransportTime, etc. Ajustar se DTO for mais complexo.
                var newRoute = new WorkCenterOperationRoute
                {
                    OperationTypeID = opTypeId,
                    Order = ++currentMaxOrder,
                    Version = 1,
                    TransportTimeInMinutes = 0, // Padrão - AJUSTAR
                    EffectiveStartDate = DateTime.UtcNow,
                    EffectiveEndDate = null
                };
                workCenter.OperationRoutes.Add(newRoute);
            }
        }
    }
}
