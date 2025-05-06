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
            _operationService = operationService ?? throw new ArgumentNullException(nameof(operationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtém todas as operações ativas.
        /// </summary>
        /// <returns>Uma lista de operações.</returns>
        /// <response code="200">Retorna a lista de operações ativas.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OperationDto>), 200)]
        [ProducesResponseType(500)] // Mantido para indicar que 500 é possível, mas tratado globalmente
        public async Task<ActionResult<IEnumerable<OperationDto>>> GetAll()
        {
            // Removido try-catch genérico. Exceções serão tratadas por middleware global.
            var operations = await _operationService.GetAllEnabledAsync();
            return Ok(operations);
        }

        /// <summary>
        /// Obtém uma operação específica pelo seu ID.
        /// </summary>
        /// <param name="id">O ID da operação.</param>
        /// <returns>A operação encontrada.</returns>
        /// <response code="200">Retorna a operação encontrada.</response>
        /// <response code="404">Se a operação não for encontrada ou estiver inativa (tratado pelo serviço/middleware).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OperationDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)] // Mantido para indicar que 500 é possível, mas tratado globalmente
        public async Task<ActionResult<OperationDto>> GetById(int id)
        {
            // Removido try-catch genérico. NotFound será retornado se o serviço retornar null
            // ou se uma exceção de "não encontrado" for lançada pelo serviço e tratada globalmente.
            var operation = await _operationService.GetByIdAsync(id);

            if (operation == null)
            {
                // O serviço GetByIdAsync já retorna null se não encontrar ou estiver inativo.
                // Se você implementar exceções customizadas no serviço (ex: NotFoundException),
                // este 'if' pode ser removido e o middleware global cuidaria do 404.
                _logger.LogWarning("Operação com ID {Id} não encontrada (ou inativa).", id);
                return NotFound($"Operação com ID {id} não encontrada.");
            }

            return Ok(operation);
        }

        /// <summary>
        /// Cria uma nova operação.
        /// </summary>
        /// <param name="createDto">Os dados para criar a nova operação.</param>
        /// <returns>A operação criada.</returns>
        /// <response code="201">Retorna a operação recém-criada.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos (validação do DTO ou do serviço/middleware).</response>
        /// <response code="409">Se houver conflito (ex: nome duplicado) (tratado pelo serviço/middleware).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpPost]
        [ProducesResponseType(typeof(OperationDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)] // Adicionado para indicar possível conflito (nome duplicado)
        [ProducesResponseType(500)] // Mantido para indicar que 500 é possível, mas tratado globalmente
        public async Task<ActionResult<OperationDto>> Create([FromBody] CreateOperationDto createDto)
        {
            // Mantém a validação inicial do ModelState
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Removido try-catch genérico e tratamento específico de exceções de validação/negócio.
            // Exceções lançadas pelo serviço (validação de IDs, nome duplicado, etc.)
            // serão tratadas por middleware global, retornando 400 ou 409.
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
        /// <response code="400">Se os dados fornecidos forem inválidos (validação do DTO ou do serviço/middleware).</response>
        /// <response code="404">Se a operação não for encontrada (tratado pelo serviço/middleware).</response>
        /// <response code="409">Se houver conflito (ex: nome duplicado) (tratado pelo serviço/middleware).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OperationDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)] // Adicionado para indicar possível conflito (nome duplicado)
        [ProducesResponseType(500)] // Mantido para indicar que 500 é possível, mas tratado globalmente
        public async Task<ActionResult<OperationDto>> Update(int id, [FromBody] UpdateOperationDto updateDto)
        {
            // Mantém a validação inicial do ModelState
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Removido try-catch genérico e tratamento específico de exceções de validação/negócio.
            // Exceções lançadas pelo serviço (validação de IDs, nome duplicado, item inativo, etc.)
            // serão tratadas por middleware global, retornando 400 ou 409.
            // O serviço UpdateAsync retornará null se não encontrar a entidade.
            var updatedOperation = await _operationService.UpdateAsync(id, updateDto);

            if (updatedOperation == null)
            {
                // O serviço UpdateAsync já retorna null se não encontrar.
                // Se você implementar exceções customizadas no serviço (ex: NotFoundException),
                // este 'if' pode ser removido e o middleware global cuidaria do 404.
                _logger.LogWarning("Operação com ID {Id} não encontrada para atualização.", id);
                return NotFound($"Operação com ID {id} não encontrada.");
            }

            return Ok(updatedOperation);
        }

        /// <summary>
        /// Desabilita (soft delete) uma operação.
        /// </summary>
        /// <param name="id">O ID da operação a ser desabilitada.</param>
        /// <returns>Nenhum conteúdo.</returns>
        /// <response code="204">Operação desabilitada com sucesso.</response>
        /// <response code="404">Se a operação não for encontrada (tratado pelo serviço/middleware).</response>
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
            var success = await _operationService.DeleteAsync(id);

            if (!success)
            {
                // O serviço DeleteAsync já retorna false se não encontrar.
                // Se você implementar exceções customizadas no serviço (ex: NotFoundException),
                // este 'if' pode ser removido e o middleware global cuidaria do 404.
                _logger.LogWarning("Operação com ID {Id} não encontrada para exclusão ou falha ao excluir.", id);
                return NotFound($"Operação com ID {id} não encontrada.");
            }

            return NoContent();
        }
    }
}
