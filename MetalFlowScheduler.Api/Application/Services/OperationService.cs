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
            // Usa o novo método do repositório que inclui detalhes para melhor performance
            var operations = await _operationRepository.GetAllEnabledWithDetailsAsync();

            // Mapeia para DTOs, o AutoMapper já está configurado para mapear OperationType.Name e WorkCenter.Name
            return _mapper.Map<IEnumerable<OperationDto>>(operations);
        }

        /// <inheritdoc/>
        public async Task<OperationDto?> GetByIdAsync(int id)
        {
            // Usa o novo método do repositório que inclui detalhes
            var operation = await _operationRepository.GetByIdWithDetailsAsync(id);

            if (operation == null || !operation.Enabled)
            {
                // Lança NotFoundException em vez de retornar null
                throw new NotFoundException($"Operação com ID {id} não encontrada (ou inativa).");
            }

            // Mapeia para DTO, o AutoMapper já está configurado para mapear OperationType.Name e WorkCenter.Name
            return _mapper.Map<OperationDto>(operation);
        }

        /// <inheritdoc/>
        public async Task<OperationDto> CreateAsync(CreateOperationDto createDto)
        {
            // Validações lançarão exceções se falharem
            await ValidateRelatedEntitiesAsync(createDto.OperationTypeId, createDto.WorkCenterId);

            // C11: Verificar nome existente (assumindo unicidade global por enquanto)
            var existingOperations = await _operationRepository.FindAsync(o => o.Name.ToLower() == createDto.Name.ToLower());
            var existingActive = existingOperations.FirstOrDefault(o => o.Enabled);
            var existingInactive = existingOperations.FirstOrDefault(o => !o.Enabled);

            if (existingActive != null)
            {
                // Lança ConflictException se já existir uma operação ativa com o mesmo nome
                throw new ConflictException($"Já existe uma Operação ativa com o nome '{createDto.Name}'.");
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

            // Retorna DTO com detalhes carregados buscando novamente (ou pode otimizar para mapear diretamente se todos os dados estiverem disponíveis)
            return await GetByIdAsync(operationToProcess.ID) ??
                  throw new Exception("Falha inesperada ao buscar a operação recém-criada/atualizada."); // Should not happen if Add/Update succeeded
        }

        /// <inheritdoc/>
        public async Task<OperationDto?> UpdateAsync(int id, UpdateOperationDto updateDto)
        {
            // Busca a operação existente. Usa GetByIdAsync que já lança NotFoundException se não encontrar/inativo.
            // Se precisar carregar detalhes para validações de negócio, use GetByIdWithDetailsAsync.
            var operation = await _operationRepository.GetByIdAsync(id);

            if (operation == null)
            {
                // Lança NotFoundException se não encontrado
                throw new NotFoundException($"Operação com ID {id} não encontrada para atualização.");
            }

            if (!operation.Enabled)
            {
                // Regra de negócio: não permitir atualizar inativos diretamente - Lança ConflictException
                throw new ConflictException($"Não é possível atualizar uma Operação inativa (ID: {id}). Considere reativá-la primeiro.");
            }

            // Validações lançarão exceções se falharem
            await ValidateRelatedEntitiesAsync(updateDto.OperationTypeId, updateDto.WorkCenterId);

            // Verificar conflito de nome com outros registros ATIVOS
            if (!string.Equals(operation.Name, updateDto.Name, StringComparison.OrdinalIgnoreCase))
            {
                var conflictingOperation = (await _operationRepository.FindAsync(o =>
                    o.Name.ToLower() == updateDto.Name.ToLower() && o.ID != id && o.Enabled))
                    .FirstOrDefault();

                if (conflictingOperation != null)
                {
                    // Lança ConflictException se houver conflito de nome
                    throw new ConflictException($"Já existe outra Operação ativa com o nome '{updateDto.Name}'.");
                }
            }

            _mapper.Map(updateDto, operation);
            await _operationRepository.UpdateAsync(operation);

            // Retorna DTO com detalhes carregados buscando novamente
            return await GetByIdAsync(operation.ID);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(int id)
        {
            var operation = await _operationRepository.GetByIdAsync(id);
            if (operation == null)
            {
                // Lança NotFoundException se não encontrado
                throw new NotFoundException($"Operação com ID {id} não encontrada para exclusão.");
            }

            if (!operation.Enabled) return true; // Já inativa, considera sucesso sem erro

            // TODO: Adicionar validações de negócio antes de deletar (ex: verificar dependências ativas)
            // Exemplo: Verificar se a operação está associada a algum ProductionOrderItem ativo
            // var hasActiveProductionOrderItems = await _productionOrderItemRepository.FindAsync(poi => poi.OperationID == id && poi.Enabled); // Assumindo que ProductionOrderItem tem OperationID
            // if (hasActiveProductionOrderItems.Any())
            // {
            //     throw new ConflictException($"Não é possível desabilitar a Operação ID {id} pois existem Itens de Ordem de Produção ativos associados a ela.");
            // }


            try
            {
                await _operationRepository.SoftRemoveAsync(operation); // Soft delete
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desabilitar operação ID {OperationId}", id);
                // Lança uma exceção genérica ou customizada se a falha for inesperada no repositório
                throw new Exception($"Falha ao desabilitar a Operação com ID {id}.", ex);
            }
        }

        /// <summary>
        /// Valida se as entidades relacionadas (OperationType, WorkCenter) existem e estão ativas.
        /// Lança ValidationException ou NotFoundException se falhar.
        /// </summary>
        private async Task ValidateRelatedEntitiesAsync(int operationTypeId, int workCenterId)
        {
            var operationType = await _operationTypeRepository.GetByIdAsync(operationTypeId);
            if (operationType == null || !operationType.Enabled)
            {
                // Lança ValidationException ou NotFoundException
                throw new ValidationException($"Tipo de Operação com ID {operationTypeId} inválido ou inativo.");
            }

            var workCenter = await _workCenterRepository.GetByIdAsync(workCenterId);
            if (workCenter == null || !workCenter.Enabled)
            {
                // Lança ValidationException ou NotFoundException
                throw new ValidationException($"Centro de Trabalho com ID {workCenterId} inválido ou inativo.");
            }
        }
    }
}
