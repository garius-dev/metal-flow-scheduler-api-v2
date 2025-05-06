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
using Microsoft.EntityFrameworkCore; // For DbUpdateException, if specific handling is needed

namespace MetalFlowScheduler.Api.Application.Services
{
    /// <summary>
    /// Service for managing operations related to the WorkCenter entity.
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
            var workCenters = await _workCenterRepository.GetAllEnabledWithDetailsAsync();
            return _mapper.Map<IEnumerable<WorkCenterDto>>(workCenters);
        }

        /// <inheritdoc/>
        public async Task<WorkCenterDto> GetByIdAsync(int id)
        {
            var workCenter = await _workCenterRepository.GetByIdWithDetailsAsync(id);

            if (workCenter == null || !workCenter.Enabled)
            {
                _logger.LogWarning("WorkCenter with ID {WorkCenterId} not found or not enabled.", id);
                throw new NotFoundException(nameof(WorkCenter), id);
            }
            return _mapper.Map<WorkCenterDto>(workCenter);
        }

        /// <inheritdoc/>
        public async Task<WorkCenterDto> CreateAsync(CreateWorkCenterDto createDto)
        {
            await ValidateLineAsync(createDto.LineId);
            await ValidateOperationTypesAsync(createDto.OperationTypeIds);

            var existingWorkCenters = await _workCenterRepository.FindAsync(wc => wc.Name.ToLower() == createDto.Name.ToLower());
            var existingActive = existingWorkCenters.FirstOrDefault(wc => wc.Enabled);
            var existingInactive = existingWorkCenters.FirstOrDefault(wc => !wc.Enabled);

            if (existingActive != null)
            {
                _logger.LogWarning("Attempted to create WorkCenter with duplicate active name: {WorkCenterName}", createDto.Name);
                throw new ConflictException($"A WorkCenter with the name '{createDto.Name}' already exists and is active.");
            }

            WorkCenter workCenterToProcess;
            bool isReactivating = false;

            if (existingInactive != null)
            {
                _logger.LogInformation("Reactivating and updating inactive WorkCenter with name: {WorkCenterName}, ID: {WorkCenterId}", createDto.Name, existingInactive.ID);
                workCenterToProcess = existingInactive;
                var existingDetails = await _workCenterRepository.GetByIdWithDetailsAsync(workCenterToProcess.ID);
                if (existingDetails?.OperationRoutes != null)
                {
                    workCenterToProcess.OperationRoutes.Clear(); // Clear existing routes on reactivation
                }
                _mapper.Map(createDto, workCenterToProcess);
                workCenterToProcess.Enabled = true;
                isReactivating = true;
            }
            else
            {
                workCenterToProcess = _mapper.Map<WorkCenter>(createDto);
            }

            ManageOperationRoutes(workCenterToProcess, createDto.OperationTypeIds, true);


            try
            {
                if (isReactivating)
                {
                    await _workCenterRepository.UpdateAsync(workCenterToProcess);
                }
                else
                {
                    await _workCenterRepository.AddAsync(workCenterToProcess);
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while creating/reactivating WorkCenter: {WorkCenterName}", createDto.Name);
                throw new Exception("A database error occurred while saving the work center.", ex);
            }

            return await GetByIdAsync(workCenterToProcess.ID);
        }

        /// <inheritdoc/>
        public async Task<WorkCenterDto> UpdateAsync(int id, UpdateWorkCenterDto updateDto)
        {
            var workCenter = await _workCenterRepository.GetByIdWithDetailsAsync(id);

            if (workCenter == null)
            {
                _logger.LogWarning("WorkCenter with ID {WorkCenterId} not found for update.", id);
                throw new NotFoundException(nameof(WorkCenter), id);
            }

            if (!workCenter.Enabled)
            {
                _logger.LogWarning("Attempted to update inactive WorkCenter with ID {WorkCenterId}.", id);
                throw new ConflictException($"Cannot update an inactive WorkCenter (ID: {id}). Consider reactivating it first.");
            }

            await ValidateLineAsync(updateDto.LineId);
            await ValidateOperationTypesAsync(updateDto.OperationTypeIds);

            if (!string.Equals(workCenter.Name, updateDto.Name, StringComparison.OrdinalIgnoreCase))
            {
                var conflictingWorkCenter = (await _workCenterRepository.FindAsync(wc =>
                    wc.Name.ToLower() == updateDto.Name.ToLower() && wc.ID != id && wc.Enabled))
                    .FirstOrDefault();
                if (conflictingWorkCenter != null)
                {
                    _logger.LogWarning("WorkCenter update for ID {WorkCenterId} resulted in a name conflict with WorkCenter ID {ConflictingWorkCenterId} for name {WorkCenterName}", id, conflictingWorkCenter.ID, updateDto.Name);
                    throw new ConflictException($"Another active WorkCenter with the name '{updateDto.Name}' already exists.");
                }
            }

            _mapper.Map(updateDto, workCenter);
            ManageOperationRoutes(workCenter, updateDto.OperationTypeIds, false);

            try
            {
                await _workCenterRepository.UpdateAsync(workCenter);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating WorkCenter ID {WorkCenterId}.", id);
                throw new Exception("A database error occurred while updating the work center.", ex);
            }

            return await GetByIdAsync(workCenter.ID);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(int id)
        {
            var workCenter = await _workCenterRepository.GetByIdAsync(id);
            if (workCenter == null)
            {
                _logger.LogWarning("WorkCenter with ID {WorkCenterId} not found for deletion.", id);
                throw new NotFoundException(nameof(WorkCenter), id, "WorkCenter not found for deletion.");
            }

            if (!workCenter.Enabled)
            {
                _logger.LogInformation("WorkCenter with ID {WorkCenterId} is already inactive.", id);
                return true;
            }

            // TODO: Add business logic validation before deletion (e.g., check for active operations or production orders)
            // if (await _operationRepository.AnyActiveOperationsForWorkCenterAsync(id))
            // {
            //    _logger.LogWarning("Attempted to delete WorkCenter ID {WorkCenterId} with active operations.", id);
            //    throw new ConflictException($"WorkCenter ID {id} cannot be disabled as it has active operations.");
            // }

            try
            {
                await _workCenterRepository.SoftRemoveAsync(workCenter);
                _logger.LogInformation("WorkCenter with ID {WorkCenterId} was successfully disabled.", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling WorkCenter ID {WorkCenterId}", id);
                throw new Exception($"An error occurred while disabling WorkCenter ID {id}.", ex);
            }
        }

        /// <summary>
        /// Validates if the Line exists and is active.
        /// Throws ValidationException if invalid.
        /// </summary>
        private async Task ValidateLineAsync(int lineId)
        {
            var line = await _lineRepository.GetByIdAsync(lineId);
            if (line == null || !line.Enabled)
            {
                _logger.LogWarning("Validation failed for LineId {LineId}: Not found or inactive.", lineId);
                throw new ValidationException(new Dictionary<string, string[]> { { "LineId", new[] { $"Line with ID {lineId} is invalid or inactive." } } });
            }
        }

        /// <summary>
        /// Validates if all OperationType IDs exist and are active.
        /// Throws ValidationException if any are invalid.
        /// </summary>
        private async Task ValidateOperationTypesAsync(List<int> operationTypeIds)
        {
            if (operationTypeIds == null || !operationTypeIds.Any())
            {
                _logger.LogWarning("Validation failed: OperationTypeIds list cannot be null or empty.");
                throw new ValidationException(new Dictionary<string, string[]> { { "OperationTypeIds", new[] { "At least one OperationType ID must be provided." } } });
            }

            var uniqueIds = operationTypeIds.Distinct().ToList();
            var operationTypes = await _operationTypeRepository.FindAsync(ot => uniqueIds.Contains(ot.ID));

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
        /// Manages the collection of WorkCenterOperationRoutes based on the IDs provided in the DTO.
        /// </summary>
        /// <param name="workCenter">The work center entity.</param>
        /// <param name="operationTypeIdsFromDto">The list of operation type IDs from the DTO.</param>
        /// <param name="isCreating">Flag to indicate if this is a create operation, affecting versioning logic.</param>
        private void ManageOperationRoutes(WorkCenter workCenter, List<int> operationTypeIdsFromDto, bool isCreating)
        {
            workCenter.OperationRoutes ??= new List<WorkCenterOperationRoute>();
            var dtoOpTypeIds = operationTypeIdsFromDto.Distinct().ToList();

            if (!isCreating) // For updates, remove routes not in DTO
            {
                var routesToRemove = workCenter.OperationRoutes
                                               .Where(r => !dtoOpTypeIds.Contains(r.OperationTypeID))
                                               .ToList();
                foreach (var route in routesToRemove)
                {
                    workCenter.OperationRoutes.Remove(route);
                }
            }

            // Add new routes or update existing ones (if versioning/details were more complex)
            // For simplicity here, we add if not present, assuming DTO provides the full desired state.
            // More complex logic would handle updates to existing route entries if DTOs carried more route-specific data.

            int currentOrder = 0; // Reset order for the new set of routes
            var existingRoutesToKeep = workCenter.OperationRoutes
                                        .Where(r => dtoOpTypeIds.Contains(r.OperationTypeID))
                                        .OrderBy(r => r.Order) // Maintain existing order for kept items if possible
                                        .ToList();

            // Clear and re-add to ensure correct ordering and handle new/removed items cleanly
            workCenter.OperationRoutes.Clear();

            foreach (var opTypeId in dtoOpTypeIds)
            {
                // For simplicity, we're creating new route objects.
                // A more sophisticated update might try to find and modify existing route objects
                // if they had more properties to update beyond just OperationTypeID and Order.
                var newRoute = new WorkCenterOperationRoute
                {
                    OperationTypeID = opTypeId,
                    Order = ++currentOrder,
                    Version = 1, // Default version, could be incremented based on more complex rules
                    TransportTimeInMinutes = 0, // Default, should come from DTO if configurable per route
                    EffectiveStartDate = DateTime.UtcNow,
                    EffectiveEndDate = null
                };
                workCenter.OperationRoutes.Add(newRoute);
            }
        }
    }
}
