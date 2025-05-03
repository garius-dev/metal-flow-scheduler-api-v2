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
using Microsoft.EntityFrameworkCore; // Para Include e DbUpdateException

namespace MetalFlowScheduler.Api.Application.Services
{
    /// <summary>
    /// Serviço para gerenciar operações relacionadas à entidade Operation.
    /// </summary>
    public class OperationService : IOperationService
    {
        private readonly IOperationRepository _operationRepository;
        private readonly IOperationTypeRepository _operationTypeRepository;
        private readonly IWorkCenterRepository _workCenterRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<OperationService> _logger;

        public OperationService(
            IOperationRepository operationRepository,
            IOperationTypeRepository operationTypeRepository,
            IWorkCenterRepository workCenterRepository,
            IMapper mapper,
            ILogger<OperationService> logger)
        {
            _operationRepository = operationRepository ?? throw new ArgumentNullException(nameof(operationRepository));
            _operationTypeRepository = operationTypeRepository ?? throw new ArgumentNullException(nameof(operationTypeRepository));
            _workCenterRepository = workCenterRepository ?? throw new ArgumentNullException(nameof(workCenterRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<OperationDto>> GetAllEnabledAsync()
        {
            // TODO: Otimizar carregamento de detalhes (OperationTypeName, WorkCenterName)
            // A abordagem atual busca separadamente, idealmente usar Include no repositório.
            var operations = await _operationRepository.FindAsync(op => op.Enabled);
            var operationTypeIds = operations.Select(o => o.OperationTypeID).Distinct().ToList();
            var workCenterIds = operations.Select(o => o.WorkCenterID).Distinct().ToList();
            var operationTypes = (await _operationTypeRepository.FindAsync(ot => operationTypeIds.Contains(ot.ID))).ToDictionary(ot => ot.ID);
            var workCenters = (await _workCenterRepository.FindAsync(wc => workCenterIds.Contains(wc.ID))).ToDictionary(wc => wc.ID);

            var dtos = operations.Select(op => {
                var dto = _mapper.Map<OperationDto>(op);
                dto.OperationTypeName = operationTypes.TryGetValue(op.OperationTypeID, out var ot) ? ot.Name : null;
                dto.WorkCenterName = workCenters.TryGetValue(op.WorkCenterID, out var wc) ? wc.Name : null;
                return dto;
            }).ToList();

            return dtos;
        }

        /// <inheritdoc/>
        public async Task<OperationDto?> GetByIdAsync(int id)
        {
            // TODO: Otimizar carregamento de detalhes (OperationTypeName, WorkCenterName) usando Include no repositório.
            var operation = await _operationRepository.GetByIdAsync(id);

            if (operation == null || !operation.Enabled)
            {
                return null;
            }

            // Carrega detalhes separadamente (solução temporária)
            var operationType = await _operationTypeRepository.GetByIdAsync(operation.OperationTypeID);
            var workCenter = await _workCenterRepository.GetByIdAsync(operation.WorkCenterID);

            var dto = _mapper.Map<OperationDto>(operation);
            dto.OperationTypeName = operationType?.Name;
            dto.WorkCenterName = workCenter?.Name;

            return dto;
        }

        /// <inheritdoc/>
        public async Task<OperationDto> CreateAsync(CreateOperationDto createDto)
        {
            await ValidateRelatedEntitiesAsync(createDto.OperationTypeId, createDto.WorkCenterId);

            // C11: Verificar nome existente (assumindo unicidade global por enquanto)
            var existingOperations = await _operationRepository.FindAsync(o => o.Name.ToLower() == createDto.Name.ToLower());
            var existingActive = existingOperations.FirstOrDefault(o => o.Enabled);
            var existingInactive = existingOperations.FirstOrDefault(o => !o.Enabled);

            if (existingActive != null)
            {
                throw new Exception($"Já existe uma Operação ativa com o nome '{createDto.Name}'."); // TODO: Use ValidationException
            }

            Operation operationToProcess;

            if (existingInactive != null)
            {
                // C11: Reativar e atualizar
                operationToProcess = existingInactive;
                _mapper.Map(createDto, operationToProcess);
                operationToProcess.Enabled = true;
                await _operationRepository.UpdateAsync(operationToProcess);
            }
            else
            {
                // Criar nova
                operationToProcess = _mapper.Map<Operation>(createDto);
                await _operationRepository.AddAsync(operationToProcess);
            }

            // Retorna DTO com detalhes carregados
            return await GetByIdAsync(operationToProcess.ID) ??
                   throw new Exception("Falha ao buscar a operação recém-criada/atualizada.");
        }

        /// <inheritdoc/>
        public async Task<OperationDto?> UpdateAsync(int id, UpdateOperationDto updateDto)
        {
            var operation = await _operationRepository.GetByIdAsync(id);

            if (operation == null) return null;

            if (!operation.Enabled)
            {
                throw new Exception($"Não é possível atualizar uma Operação inativa (ID: {id})."); // TODO: Use ValidationException
            }

            await ValidateRelatedEntitiesAsync(updateDto.OperationTypeId, updateDto.WorkCenterId);

            // Verificar conflito de nome com outros registros ATIVOS
            if (!string.Equals(operation.Name, updateDto.Name, StringComparison.OrdinalIgnoreCase))
            {
                var conflictingOperation = (await _operationRepository.FindAsync(o =>
                    o.Name.ToLower() == updateDto.Name.ToLower() && o.ID != id && o.Enabled))
                    .FirstOrDefault();

                if (conflictingOperation != null)
                {
                    throw new Exception($"Já existe outra Operação ativa com o nome '{updateDto.Name}'."); // TODO: Use ValidationException
                }
            }

            _mapper.Map(updateDto, operation);
            await _operationRepository.UpdateAsync(operation);

            // Retorna DTO com detalhes carregados
            return await GetByIdAsync(operation.ID);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(int id)
        {
            var operation = await _operationRepository.GetByIdAsync(id);
            if (operation == null) return false;
            if (!operation.Enabled) return true; // Já inativa

            // TODO: Adicionar validações de negócio antes de deletar

            try
            {
                await _operationRepository.SoftRemoveAsync(operation);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desabilitar operação ID {OperationId}", id);
                return false;
            }
        }

        /// <summary>
        /// Valida se as entidades relacionadas (OperationType, WorkCenter) existem e estão ativas.
        /// </summary>
        private async Task ValidateRelatedEntitiesAsync(int operationTypeId, int workCenterId)
        {
            var operationType = await _operationTypeRepository.GetByIdAsync(operationTypeId);
            if (operationType == null || !operationType.Enabled)
            {
                throw new Exception($"Tipo de Operação com ID {operationTypeId} inválido ou inativo."); // TODO: Use ValidationException
            }

            var workCenter = await _workCenterRepository.GetByIdAsync(workCenterId);
            if (workCenter == null || !workCenter.Enabled)
            {
                throw new Exception($"Centro de Trabalho com ID {workCenterId} inválido ou inativo."); // TODO: Use ValidationException
            }
        }
    }
}
