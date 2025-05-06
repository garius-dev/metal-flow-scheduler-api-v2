using Asp.Versioning;
using MetalFlowScheduler.Api.Application.Dtos;
using MetalFlowScheduler.Api.Application.Interfaces;
using MetalFlowScheduler.Api.Application.Exceptions; // For explicit documentation of exceptions
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System; // For ArgumentNullException
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MetalFlowScheduler.Api.WebApi.Controllers.v1
{
    /// <summary>
    /// Controller for managing WorkCenters.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    // [Authorize(Roles = "Administrator,Planner")] // Example authorization
    public class WorkCentersController : ControllerBase
    {
        private readonly IWorkCenterService _workCenterService;
        private readonly ILogger<WorkCentersController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkCentersController"/> class.
        /// </summary>
        /// <param name="workCenterService">The work center service.</param>
        /// <param name="logger">The logger.</param>
        public WorkCentersController(IWorkCenterService workCenterService, ILogger<WorkCentersController> logger)
        {
            _workCenterService = workCenterService ?? throw new ArgumentNullException(nameof(workCenterService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all enabled work centers.
        /// </summary>
        /// <returns>A list of enabled work centers.</returns>
        /// <response code="200">Returns the list of enabled work centers.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<WorkCenterDto>), 200)]
        public async Task<IActionResult> GetAllEnabled()
        {
            _logger.LogInformation("Attempting to get all enabled work centers.");
            var workCenters = await _workCenterService.GetAllEnabledAsync();
            _logger.LogInformation("Successfully retrieved all enabled work centers.");
            return Ok(workCenters);
        }

        /// <summary>
        /// Gets a specific work center by its ID.
        /// </summary>
        /// <param name="id">The ID of the work center.</param>
        /// <returns>The work center if found.</returns>
        /// <response code="200">Returns the requested work center.</response>
        /// <response code="404">If the work center is not found or not enabled.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(WorkCenterDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("Attempting to get work center with ID: {WorkCenterId}", id);
            var workCenter = await _workCenterService.GetByIdAsync(id);
            // NotFoundException will be handled by middleware
            _logger.LogInformation("Successfully retrieved work center with ID: {WorkCenterId}", id);
            return Ok(workCenter);
        }

        /// <summary>
        /// Creates a new work center.
        /// </summary>
        /// <param name="createDto">The work center creation data.</param>
        /// <returns>The created work center.</returns>
        /// <response code="201">Returns the newly created work center.</response>
        /// <response code="400">If the input data is invalid (DTO validation or business rule violation).</response>
        /// <response code="409">If a work center with the same name already exists and is active, or if related entities are invalid.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpPost]
        [ProducesResponseType(typeof(WorkCenterDto), 201)]
        [ProducesResponseType(400)] // From ValidationException or ModelState
        [ProducesResponseType(409)] // From ConflictException
        public async Task<IActionResult> Create([FromBody] CreateWorkCenterDto createDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create work center request failed due to invalid model state.");
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Attempting to create a new work center with name: {WorkCenterName}", createDto.Name);
            var createdWorkCenter = await _workCenterService.CreateAsync(createDto);
            _logger.LogInformation("Successfully created work center with ID: {WorkCenterId} and name: {WorkCenterName}", createdWorkCenter.Id, createdWorkCenter.Name);
            return CreatedAtAction(nameof(GetById), new { id = createdWorkCenter.Id, version = "1.0" }, createdWorkCenter);
        }

        /// <summary>
        /// Updates an existing work center.
        /// </summary>
        /// <param name="id">The ID of the work center to update.</param>
        /// <param name="updateDto">The work center update data.</param>
        /// <returns>The updated work center.</returns>
        /// <response code="200">Returns the updated work center.</response>
        /// <response code="400">If the input data is invalid (DTO validation or business rule violation).</response>
        /// <response code="404">If the work center to update is not found.</response>
        /// <response code="409">If the update causes a name conflict with another active work center, if the work center is inactive, or if related entities are invalid.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(WorkCenterDto), 200)]
        [ProducesResponseType(400)] // From ValidationException or ModelState
        [ProducesResponseType(404)] // From NotFoundException
        [ProducesResponseType(409)] // From ConflictException
        public async Task<IActionResult> Update(int id, [FromBody] UpdateWorkCenterDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Update work center request for ID {WorkCenterId} failed due to invalid model state.", id);
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Attempting to update work center with ID: {WorkCenterId}", id);
            var updatedWorkCenter = await _workCenterService.UpdateAsync(id, updateDto);
            _logger.LogInformation("Successfully updated work center with ID: {WorkCenterId}", id);
            return Ok(updatedWorkCenter);
        }

        /// <summary>
        /// Disables (soft deletes) a work center.
        /// </summary>
        /// <param name="id">The ID of the work center to disable.</param>
        /// <returns>No content if successful.</returns>
        /// <response code="204">Work center disabled successfully.</response>
        /// <response code="404">If the work center to disable is not found.</response>
        /// <response code="409">If the work center cannot be disabled due to business rule violations (e.g., active dependencies).</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)] // From NotFoundException
        [ProducesResponseType(409)] // From ConflictException
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Attempting to disable work center with ID: {WorkCenterId}", id);
            await _workCenterService.DeleteAsync(id);
            _logger.LogInformation("Successfully disabled work center with ID: {WorkCenterId}", id);
            return NoContent();
        }
    }
}
