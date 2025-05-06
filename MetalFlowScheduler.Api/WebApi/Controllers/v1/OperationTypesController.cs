using Asp.Versioning;
using MetalFlowScheduler.Api.Application.Dtos;
using MetalFlowScheduler.Api.Application.Interfaces;
// TODO: using MetalFlowScheduler.Api.Application.Exceptions; // Para exceções customizadas - Serão lançadas pelo serviço
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
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
            _operationTypeService = operationTypeService ?? throw new ArgumentNullException(nameof(operationTypeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtém todos os tipos de operação ativos.
        /// </summary>
        /// <returns>Uma lista de tipos de operação.</returns>
        /// <response code="200">Retorna a lista de tipos de operação ativos.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OperationTypeDto>), 200)]
        [ProducesResponseType(500)] // Mantido para indicar que 500 é possível, mas tratado globalmente
        public async Task<ActionResult<IEnumerable<OperationTypeDto>>> GetAll()
        {
            // Removido try-catch genérico. Exceções serão tratadas por middleware global.
            var operationTypes = await _operationTypeService.GetAllEnabledAsync();
            return Ok(operationTypes);
        }

        /// <summary>
        /// Obtém um tipo de operação específico pelo seu ID.
        /// </summary>
        /// <param name="id">O ID do tipo de operação.</param>
        /// <returns>O tipo de operação encontrado.</returns>
        /// <response code="200">Retorna o tipo de operação encontrado.</response>
        /// <response code="404">Se o tipo de operação não for encontrado ou estiver inativo (tratado pelo serviço/middleware).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OperationTypeDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)] // Mantido para indicar que 500 é possível, mas tratado globalmente
        public async Task<ActionResult<OperationTypeDto>> GetById(int id)
        {
            // Removido try-catch genérico. NotFound será retornado se o serviço retornar null
            // ou se uma exceção de "não encontrado" for lançada pelo serviço e tratada globalmente.
            var operationType = await _operationTypeService.GetByIdAsync(id);

            if (operationType == null)
            {
                // O serviço GetByIdAsync já retorna null se não encontrar ou estiver inativo.
                // Se você implementar exceções customizadas no serviço (ex: NotFoundException),
                // este 'if' pode ser removido e o middleware global cuidaria do 404.
                _logger.LogWarning("Tipo de Operação com ID {Id} não encontrado (ou inativo).", id);
                return NotFound($"Tipo de Operação com ID {id} não encontrado.");
            }

            return Ok(operationType);
        }

        /// <summary>
        /// Cria um novo tipo de operação.
        /// </summary>
        /// <param name="createDto">Os dados para criar o novo tipo de operação.</param>
        /// <returns>O tipo de operação criado.</returns>
        /// <response code="201">Retorna o tipo de operação recém-criado.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos (validação do DTO ou do serviço/middleware).</response>
        /// <response code="409">Se houver conflito (ex: nome duplicado) (tratado pelo serviço/middleware).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpPost]
        [ProducesResponseType(typeof(OperationTypeDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)] // Adicionado para indicar possível conflito (nome duplicado)
        [ProducesResponseType(500)] // Mantido para indicar que 500 é possível, mas tratado globalmente
        public async Task<ActionResult<OperationTypeDto>> Create([FromBody] CreateOperationTypeDto createDto)
        {
            // Mantém a validação inicial do ModelState
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Removido try-catch genérico e tratamento específico de exceções de validação/negócio.
            // Exceções lançadas pelo serviço (nome duplicado, etc.)
            // serão tratadas por middleware global, retornando 400 ou 409.
            var createdOperationType = await _operationTypeService.CreateAsync(createDto);

            return CreatedAtAction(nameof(GetById), new { id = createdOperationType.Id, version = "1.0" }, createdOperationType);
        }

        /// <summary>
        /// Atualiza um tipo de operação existente.
        /// </summary>
        /// <param name="id">O ID do tipo de operação a ser atualizado.</param>
        /// <param name="updateDto">Os novos dados para o tipo de operação.</param>
        /// <returns>O tipo de operação atualizado.</returns>
        /// <response code="200">Retorna o tipo de operação atualizado.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos (validação do DTO ou do serviço/middleware).</response>
        /// <response code="404">Se o tipo de operação não for encontrado (tratado pelo serviço/middleware).</response>
        /// <response code="409">Se houver conflito (ex: nome duplicado) (tratado pelo serviço/middleware).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OperationTypeDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)] // Adicionado para indicar possível conflito (nome duplicado)
        [ProducesResponseType(500)] // Mantido para indicar que 500 é possível, mas tratado globalmente
        public async Task<IActionResult> Update(int id, [FromBody] UpdateOperationTypeDto updateDto)
        {
            // Mantém a validação inicial do ModelState
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Removido try-catch genérico e tratamento específico de exceções de validação/negócio.
            // Exceções lançadas pelo serviço (nome duplicado, item inativo, etc.)
            // serão tratadas por middleware global, retornando 400 ou 409.
            // O serviço UpdateAsync retornará null se não encontrar a entidade.
            var updatedOperationType = await _operationTypeService.UpdateAsync(id, updateDto);

            if (updatedOperationType == null)
            {
                // O serviço UpdateAsync já retorna null se não encontrar.
                // Se você implementar exceções customizadas no serviço (ex: NotFoundException),
                // este 'if' pode ser removido e o middleware global cuidaria do 404.
                _logger.LogWarning("Tipo de Operação com ID {Id} não encontrado para atualização.", id);
                return NotFound($"Tipo de Operação com ID {id} não encontrado.");
            }

            return Ok(updatedOperationType);
        }

        /// <summary>
        /// Desabilita (soft delete) um tipo de operação.
        /// </summary>
        /// <param name="id">O ID do tipo de operação a ser desabilitado.</param>
        /// <returns>Nenhum conteúdo.</returns>
        /// <response code="204">Tipo de operação desabilitado com sucesso.</response>
        /// <response code="404">Se o tipo de operação não for encontrado (tratado pelo serviço/middleware).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)] // Mantido para indicar que 500 é possível, mas tratado globalmente
        public async Task<IActionResult> Delete(int id)
        {
            // Removido try-catch genérico. Exceções lançadas pelo serviço
            // serão tratadas por middleware global.
            // O serviço DeleteAsync retornará false se não encontrar a entidade.
            var success = await _operationTypeService.DeleteAsync(id);

            if (!success)
            {
                // O serviço DeleteAsync já retorna false se não encontrar.
                // Se você implementar exceções customizadas no serviço (ex: NotFoundException),
                // este 'if' pode ser removido e o middleware global cuidaria do 404.
                _logger.LogWarning("Tipo de Operação com ID {Id} não encontrado para exclusão ou falha ao excluir.", id);
                return NotFound($"Tipo de Operação com ID {id} não encontrado.");
            }

            return NoContent();
        }
    }
}
