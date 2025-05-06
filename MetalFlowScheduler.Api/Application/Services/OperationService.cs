// Arquivo: Application/Services/OperationService.cs
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
            var operations = await _operationRepository.GetAllEnabledWithDetailsAsync();
            return _mapper.Map<IEnumerable<OperationDto>>(operations);
        }

        /// <inheritdoc/>
        public async Task<OperationDto> GetByIdAsync(int id)
        {
            var operation = await _operationRepository.GetByIdWithDetailsAsync(id);

            if (operation == null || !operation.Enabled)
            {
                throw new NotFoundException($"Operação com ID {id} não encontrada (ou inativa).");
            }

            return _mapper.Map<OperationDto>(operation);
        }

        /// <inheritdoc/>
        public async Task<OperationDto> CreateAsync(CreateOperationDto createDto)
        {
            await ValidateRelatedEntitiesAsync(createDto.OperationTypeId, createDto.WorkCenterId);

            var existingOperations = await _operationRepository.FindAsync(o => o.Name.ToLower() == createDto.Name.ToLower());
            var existingActive = existingOperations.FirstOrDefault(o => o.Enabled);
            var existingInactive = existingOperations.FirstOrDefault(o => !o.Enabled);

            if (existingActive != null)
            {
                throw new ConflictException($"Já existe uma Operação ativa com o nome '{createDto.Name}'.");
            }

            Operation operationToProcess;

            if (existingInactive != null)
            {
                operationToProcess = existingInactive;
                _mapper.Map(createDto, operationToProcess);
                operationToProcess.Enabled = true;
                await _operationRepository.UpdateAsync(operationToProcess);
            }
            else
            {
                operationToProcess = _mapper.Map<Operation>(createDto);
                await _operationRepository.AddAsync(operationToProcess);
            }

            return await GetByIdAsync(operationToProcess.ID);
        }

        /// <inheritdoc/>
        public async Task<OperationDto> UpdateAsync(int id, UpdateOperationDto updateDto)
        {
            var operation = await _operationRepository.GetByIdAsync(id);

            if (operation == null)
            {
                throw new NotFoundException($"Operação com ID {id} não encontrada para atualização.");
            }

            if (!operation.Enabled)
            {
                throw new ConflictException($"Não é possível atualizar uma Operação inativa (ID: {id}). Considere reativá-la primeiro.");
            }

            await ValidateRelatedEntitiesAsync(updateDto.OperationTypeId, updateDto.WorkCenterId);

            if (!string.Equals(operation.Name, updateDto.Name, StringComparison.OrdinalIgnoreCase))
            {
                var conflictingOperation = (await _operationRepository.FindAsync(o =>
                    o.Name.ToLower() == updateDto.Name.ToLower() && o.ID != id && o.Enabled))
                    .FirstOrDefault();

                if (conflictingOperation != null)
                {
                    throw new ConflictException($"Já existe outra Operação ativa com o nome '{updateDto.Name}'.");
                }
            }

            _mapper.Map(updateDto, operation);
            await _operationRepository.UpdateAsync(operation);

            return await GetByIdAsync(operation.ID);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(int id)
        {
            var operation = await _operationRepository.GetByIdAsync(id);
            if (operation == null)
            {
                throw new NotFoundException($"Operação com ID {id} não encontrada para exclusão.");
            }

            if (!operation.Enabled) return true;

            // TODO: Add business validations before deleting (e.g., check for active dependencies)

            try
            {
                await _operationRepository.SoftRemoveAsync(operation);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desabilitar operação ID {OperationId}", id);
                throw new Exception($"Falha ao desabilitar a Operação com ID {id}.", ex); // Consider throwing a more specific custom exception if needed
            }
        }

        /// <summary>
        /// Valida se as entidades relacionadas (OperationType, WorkCenter) existem e estão ativas.
        /// Lança ValidationException se falhar.
        /// </summary>
        private async Task ValidateRelatedEntitiesAsync(int operationTypeId, int workCenterId)
        {
            var operationType = await _operationTypeRepository.GetByIdAsync(operationTypeId);
            if (operationType == null || !operationType.Enabled)
            {
                throw new ValidationException($"Tipo de Operação com ID {operationTypeId} inválido ou inativo.");
            }

            var workCenter = await _workCenterRepository.GetByIdAsync(workCenterId);
            if (workCenter == null || !workCenter.Enabled)
            {
                throw new ValidationException($"Centro de Trabalho com ID {workCenterId} inválido ou inativo.");
            }
        }
    }
}