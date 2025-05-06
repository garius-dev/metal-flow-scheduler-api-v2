using MetalFlowScheduler.Api.Application.Dtos;
using MetalFlowScheduler.Api.Application.Exceptions; // Required for exception documentation
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MetalFlowScheduler.Api.Application.Interfaces
{
    /// <summary>
    /// Interface for the service of managing WorkCenters.
    /// </summary>
    public interface IWorkCenterService
    {
        /// <summary>
        /// Gets all enabled work centers.
        /// </summary>
        /// <returns>A collection of enabled work center DTOs.</returns>
        Task<IEnumerable<WorkCenterDto>> GetAllEnabledAsync();

        /// <summary>
        /// Gets a work center by its ID.
        /// </summary>
        /// <param name="id">The ID of the work center.</param>
        /// <returns>The work center DTO if found and enabled.</returns>
        /// <exception cref="NotFoundException">Thrown if the work center is not found or is not enabled.</exception>
        Task<WorkCenterDto> GetByIdAsync(int id);

        /// <summary>
        /// Creates a new work center, including its operation routes.
        /// </summary>
        /// <param name="createDto">The data to create the new work center.</param>
        /// <returns>The created work center DTO.</returns>
        /// <exception cref="ConflictException">Thrown if a work center with the same name already exists and is active.</exception>
        /// <exception cref="ValidationException">Thrown if related entities (e.g., Line, OperationTypes) are invalid or not found.</exception>
        Task<WorkCenterDto> CreateAsync(CreateWorkCenterDto createDto);

        /// <summary>
        /// Updates an existing work center, managing its operation routes.
        /// </summary>
        /// <param name="id">The ID of the work center to update.</param>
        /// <param name="updateDto">The new data for the work center.</param>
        /// <returns>The updated work center DTO.</returns>
        /// <exception cref="NotFoundException">Thrown if the work center is not found.</exception>
        /// <exception cref="ConflictException">Thrown if the work center is inactive or if a work center with the same name already exists and is active.</exception>
        /// <exception cref="ValidationException">Thrown if related entities (e.g., Line, OperationTypes) are invalid or not found.</exception>
        Task<WorkCenterDto> UpdateAsync(int id, UpdateWorkCenterDto updateDto);

        /// <summary>
        /// Disables (soft delete) a work center.
        /// </summary>
        /// <param name="id">The ID of the work center to disable.</param>
        /// <returns>True if the work center was successfully disabled or was already inactive.</returns>
        /// <exception cref="NotFoundException">Thrown if the work center is not found.</exception>
        /// <exception cref="ConflictException">Thrown if there are business rule violations preventing deletion (e.g., active dependencies).</exception>
        Task<bool> DeleteAsync(int id);
    }
}
