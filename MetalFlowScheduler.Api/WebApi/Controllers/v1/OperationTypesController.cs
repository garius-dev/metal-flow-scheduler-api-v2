// Arquivo: WebApi/Controllers/v1/OperationTypesController.cs
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
    /// Controlador para gerenciar Tipos de Operação.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class OperationTypesController : ControllerBase
    {
        private readonly IOperationTypeService _operationTypeService;
        private readonly ILogger<OperationTypesController> _logger;

        public OperationTypesController(IOperationTypeService operationTypeService, ILogger<OperationTypesController> logger)
        {
            _operationTypeService = operationTypeService;
            _logger = logger;
        }

        /// <summary>
        /// Obtém todos os tipos de operação ativos.
        /// </summary>
        /// <returns>Uma lista de tipos de operação.</returns>
        /// <response code="200">Retorna a lista de tipos de operação ativos.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente pelo middleware).</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OperationTypeDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<OperationTypeDto>>> GetAll()
        {
            var operationTypes = await _operationTypeService.GetAllEnabledAsync();
            return Ok(operationTypes);
        }

        /// <summary>
        /// Obtém um tipo de operação específico pelo seu ID.
        /// </summary>
        /// <param name="id">O ID do tipo de operação.</param>
        /// <returns>O tipo de operação encontrado.</returns>
        /// <response code="200">Retorna o tipo de operação encontrado.</response>
        /// <response code="404">Se o tipo de operação não for encontrado ou estiver inativo (tratado pelo middleware via NotFoundException).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente pelo middleware).</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OperationTypeDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<OperationTypeDto>> GetById(int id)
        {
            // Service will throw NotFoundException if not found/inactive, middleware handles 404.
            var operationType = await _operationTypeService.GetByIdAsync(id);
            return Ok(operationType);
        }

        /// <summary>
        /// Cria um novo tipo de operação.
        /// </summary>
        /// <param name="createDto">Os dados para criar o novo tipo de operação.</param>
        /// <returns>O tipo de operação criado.</returns>
        /// <response code="201">Retorna o tipo de operação recém-criado.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos (validação do DTO ou tratado pelo middleware via ValidationException).</response>
        /// <response code="409">Se houver conflito (ex: nome duplicado) (tratado pelo middleware via ConflictException).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente pelo middleware).</response>
        [HttpPost]
        [ProducesResponseType(typeof(OperationTypeDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<OperationTypeDto>> Create([FromBody] CreateOperationTypeDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Service will throw ConflictException if name exists, middleware handles 409.
            var createdOperationType = await _operationTypeService.CreateAsync(createDto);

            return CreatedAtAction(nameof(GetById), new { id = createdOperationType.Id, version = "1.0" }, createdOperationType);
        }

        /// <summary>
        /// Atualiza um tipo de operação existente.
        /// </summary>
        /// <param name="id">O ID do tipo de operação a ser atualizada.</param>
        /// <param name="updateDto">Os novos dados para o tipo de operação.</param>
        /// <returns>O objeto atualizado.</returns>
        /// <response code="200">Retorna a operação atualizada.</response>
        /// <response code="400">If the provided data is invalid.</response>
        /// <response code="404">If the operation type is not found (handled by middleware via NotFoundException).</response>
        /// <response code="409">If there is a conflict (e.g., duplicate name or inactive item) (handled by middleware via ConflictException).</response>
        /// <response code="500">If an unexpected server error occurs (handled globally by middleware).</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OperationTypeDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateOperationTypeDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Service will throw NotFoundException or ConflictException, middleware handles them.
            var updatedOperationType = await _operationTypeService.UpdateAsync(id, updateDto);

            return Ok(updatedOperationType);
        }

        /// <summary>
        /// Desabilita (soft delete) um tipo de operação.
        /// </summary>
        /// <param name="id">O ID do tipo de operação a ser desabilitado.</param>
        /// <returns>Nenhum conteúdo.</returns>
        /// <response code="204">Tipo de operação desabilitado com sucesso.</response>
        /// <response code="404">Se o tipo de operação não for encontrado (tratado pelo middleware via NotFoundException).</response>
        /// <response code="409">Se houver conflito (ex: dependências ativas) (tratado pelo middleware via ConflictException).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente pelo middleware).</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Delete(int id)
        {
            // Service will throw NotFoundException or ConflictException, middleware handles them.
            var success = await _operationTypeService.DeleteAsync(id);

            // NoContent is returned if the service indicates success (including if already inactive)
            return NoContent();
        }
    }
}