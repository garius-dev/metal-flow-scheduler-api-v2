// Arquivo: WebApi/Controllers/v1/OperationsController.cs
using Asp.Versioning;
using MetalFlowScheduler.Api.Application.Dtos;
using MetalFlowScheduler.Api.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MetalFlowScheduler.Api.WebApi.Controllers.v1
{
    /// <summary>
    /// Controlador para gerenciar Operações.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class OperationsController : ControllerBase
    {
        private readonly IOperationService _operationService;
        private readonly ILogger<OperationsController> _logger;

        public OperationsController(IOperationService operationService, ILogger<OperationsController> logger)
        {
            _operationService = operationService;
            _logger = logger;
        }

        /// <summary>
        /// Obtém todas as operações ativas.
        /// </summary>
        /// <returns>Uma lista de operações.</returns>
        /// <response code="200">Retorna a lista de operações ativas.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente pelo middleware).</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OperationDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<OperationDto>>> GetAll()
        {
            var operations = await _operationService.GetAllEnabledAsync();
            return Ok(operations);
        }

        /// <summary>
        /// Obtém uma operação específica pelo seu ID.
        /// </summary>
        /// <param name="id">O ID da operação.</param>
        /// <returns>A operação encontrada.</returns>
        /// <response code="200">Retorna a operação encontrada.</response>
        /// <response code="404">Se a operação não for encontrada ou estiver inativa (tratado pelo middleware via NotFoundException).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente pelo middleware).</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OperationDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<OperationDto>> GetById(int id)
        {
            // Service will throw NotFoundException if not found/inactive, middleware handles 404.
            var operation = await _operationService.GetByIdAsync(id);
            return Ok(operation);
        }

        /// <summary>
        /// Cria uma nova operação.
        /// </summary>
        /// <param name="createDto">Os dados para criar a nova operação.</param>
        /// <returns>A operação criada.</returns>
        /// <response code="201">Retorna a operação recém-criada.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos (validação do DTO ou tratado pelo middleware via ValidationException).</response>
        /// <response code="409">Se houver conflito (ex: nome duplicado) (tratado pelo middleware via ConflictException).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente pelo middleware).</response>
        [HttpPost]
        [ProducesResponseType(typeof(OperationDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<OperationDto>> Create([FromBody] CreateOperationDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Service will throw exceptions (Validation, Conflict, etc.), middleware handles them.
            var createdOperation = await _operationService.CreateAsync(createDto);

            return CreatedAtAction(nameof(GetById), new { id = createdOperation.Id, version = "1.0" }, createdOperation);
        }

        /// <summary>
        /// Atualiza uma operação existente.
        /// </summary>
        /// <param name="id">O ID da operação a ser atualizada.</param>
        /// <param name="updateDto">Os novos dados para a operação.</param>
        /// <returns>O objeto atualizado.</returns>
        /// <response code="200">Retorna a operação atualizada.</response>
        /// <response code="400">If the provided data is invalid.</response>
        /// <response code="404">If the operation is not found (handled by middleware via NotFoundException).</response>
        /// <response code="409">If there is a conflict (e.g., duplicate name or inactive item) (handled by middleware via ConflictException).</response>
        /// <response code="500">If an unexpected server error occurs (handled globally by middleware).</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OperationDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<OperationDto>> Update(int id, [FromBody] UpdateOperationDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Service will throw exceptions (NotFound, Conflict, Validation, etc.), middleware handles them.
            var updatedOperation = await _operationService.UpdateAsync(id, updateDto);

            return Ok(updatedOperation);
        }

        /// <summary>
        /// Desabilita (soft delete) uma operação.
        /// </summary>
        /// <param name="id">O ID da operação a ser desabilitada.</param>
        /// <returns>Nenhum conteúdo.</returns>
        /// <response code="204">Operação desabilitada com sucesso.</response>
        /// <response code="404">Se a operação não for encontrada (tratado pelo middleware via NotFoundException).</response>
        /// <response code="409">Se houver conflito (ex: dependências ativas) (tratado pelo middleware via ConflictException).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente pelo middleware).</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Delete(int id)
        {
            // Service will throw exceptions (NotFound, Conflict, etc.), middleware handles them.
            var success = await _operationService.DeleteAsync(id);

            return NoContent(); // Returns 204 if the service indicates success (including if already inactive)
        }
    }
}