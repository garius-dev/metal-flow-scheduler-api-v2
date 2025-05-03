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

namespace MetalFlowScheduler.Api.Application.Services
{
    /// <summary>
    /// Serviço para gerenciar operações relacionadas à entidade OperationType.
    /// </summary>
    public class OperationTypeService : IOperationTypeService
    {
        private readonly IOperationTypeRepository _operationTypeRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<OperationTypeService> _logger;

        public OperationTypeService(
            IOperationTypeRepository operationTypeRepository,
            IMapper mapper,
            ILogger<OperationTypeService> logger)
        {
            _operationTypeRepository = operationTypeRepository ?? throw new ArgumentNullException(nameof(operationTypeRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<OperationTypeDto>> GetAllEnabledAsync()
        {
            var operationTypes = await _operationTypeRepository.GetAllEnabledAsync();
            return _mapper.Map<IEnumerable<OperationTypeDto>>(operationTypes);
        }

        /// <inheritdoc/>
        public async Task<OperationTypeDto?> GetByIdAsync(int id)
        {
            var operationType = await _operationTypeRepository.GetByIdAsync(id);

            if (operationType == null || !operationType.Enabled)
            {
                return null; // Retorna null se não encontrado ou inativo
            }
            return _mapper.Map<OperationTypeDto>(operationType);
        }

        /// <inheritdoc/>
        public async Task<OperationTypeDto> CreateAsync(CreateOperationTypeDto createDto)
        {
            // R01 & C11: Verificar nome existente (case-insensitive)
            var existingOperationTypes = await _operationTypeRepository.FindAsync(ot => ot.Name.ToLower() == createDto.Name.ToLower());
            var existingActive = existingOperationTypes.FirstOrDefault(ot => ot.Enabled);
            var existingInactive = existingOperationTypes.FirstOrDefault(ot => !ot.Enabled);

            if (existingActive != null)
            {
                // Viola R01 (nome único ativo)
                throw new Exception($"Já existe um Tipo de Operação ativo com o nome '{createDto.Name}'."); // TODO: Use ValidationException
            }

            OperationType operationTypeToProcess;

            if (existingInactive != null)
            {
                // C11: Reativar e atualizar item inativo existente
                operationTypeToProcess = existingInactive;
                _mapper.Map(createDto, operationTypeToProcess);
                operationTypeToProcess.Enabled = true;
                await _operationTypeRepository.UpdateAsync(operationTypeToProcess);
            }
            else
            {
                // Criar novo item
                operationTypeToProcess = _mapper.Map<OperationType>(createDto);
                await _operationTypeRepository.AddAsync(operationTypeToProcess);
            }

            return _mapper.Map<OperationTypeDto>(operationTypeToProcess);
        }

        /// <inheritdoc/>
        public async Task<OperationTypeDto?> UpdateAsync(int id, UpdateOperationTypeDto updateDto)
        {
            var operationType = await _operationTypeRepository.GetByIdAsync(id);

            if (operationType == null) return null; // Não encontrado

            if (!operationType.Enabled)
            {
                // Regra de negócio: não permitir atualizar inativos diretamente
                throw new Exception($"Não é possível atualizar um Tipo de Operação inativo (ID: {id})."); // TODO: Use ValidationException
            }

            // R01: Verificar conflito de nome com outros registros ATIVOS
            if (!string.Equals(operationType.Name, updateDto.Name, StringComparison.OrdinalIgnoreCase))
            {
                var conflictingOperationType = (await _operationTypeRepository.FindAsync(ot =>
                    ot.Name.ToLower() == updateDto.Name.ToLower() && ot.ID != id && ot.Enabled))
                    .FirstOrDefault();

                if (conflictingOperationType != null)
                {
                    throw new Exception($"Já existe outro Tipo de Operação ativo com o nome '{updateDto.Name}'."); // TODO: Use ValidationException
                }
            }

            _mapper.Map(updateDto, operationType);
            await _operationTypeRepository.UpdateAsync(operationType);

            return _mapper.Map<OperationTypeDto>(operationType);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(int id)
        {
            var operationType = await _operationTypeRepository.GetByIdAsync(id);
            if (operationType == null) return false; // Não encontrado
            if (!operationType.Enabled) return true; // Já inativo, considera sucesso

            // TODO: Adicionar validações de negócio antes de deletar (ex: verificar dependências ativas)

            try
            {
                await _operationTypeRepository.SoftRemoveAsync(operationType); // Soft delete
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desabilitar tipo de operação ID {OperationTypeId}", id);
                return false; // Falha
            }
        }
    }
}
