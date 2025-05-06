// Arquivo: WebApi/Controllers/v1/OperationsController.cs
using Asp.Versioning;
using MetalFlowScheduler.Api.Application.Dtos;
using MetalFlowScheduler.Api.Application.Interfaces;
// TODO: using MetalFlowScheduler.Api.Application.Exceptions; // Não precisa mais importar aqui, middleware trata
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
// Pode ser útil para lançar BadRequest(ModelState) como ValidationException, mas não é estritamente necessário para este refactoring.
// using MetalFlowScheduler.Api.Application.Exceptions;

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
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente pelo middleware).</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OperationDto>), 200)]
        [ProducesResponseType(500)] // Mantido para documentação
        public async Task<ActionResult<IEnumerable<OperationDto>>> GetAll()
        {
            // Não precisa de try-catch. Exceções do serviço (incluindo 500) serão tratadas pelo middleware.
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
        [ProducesResponseType(404)] // Documenta a resposta 404 esperada
        [ProducesResponseType(500)] // Mantido para documentação
        public async Task<ActionResult<OperationDto>> GetById(int id)
        {
            // Não precisa de try-catch ou if (operation == null).
            // O serviço lançará NotFoundException se não encontrar ou estiver inativa,
            // e o middleware converterá para 404.
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
        [ProducesResponseType(409)] // Documenta a resposta 409 esperada
        [ProducesResponseType(500)] // Mantido para documentação
        public async Task<ActionResult<OperationDto>> Create([FromBody] CreateOperationDto createDto)
        {
            // Mantém a validação inicial do ModelState.
            // Opcional: pode-se converter ModelState.IsValid em uma ValidationException aqui.
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // Retorna 400 com detalhes do ModelState
                // Ou, para consistência com o middleware:
                // throw new ValidationException(ModelState.Where(m => m.Value.Errors.Any()).ToDictionary(
                //     kvp => kvp.Key,
                //     kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                // ));
            }

            // Não precisa de try-catch. Exceções do serviço (ValidationException, ConflictException, etc.)
            // serão tratadas pelo middleware.
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
        /// <response code="400">Se os dados fornecidos forem inválidos (validação do DTO ou tratado pelo middleware).</response>
        /// <response code="404">Se a operação não for encontrada (tratado pelo middleware via NotFoundException).</response>
        /// <response code="409">Se houver conflito (ex: nome duplicado ou item inativo) (tratado pelo middleware via ConflictException).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente pelo middleware).</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OperationDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)] // Documenta a resposta 404 esperada
        [ProducesResponseType(409)] // Documenta a resposta 409 esperada
        [ProducesResponseType(500)] // Mantido para documentação
        public async Task<ActionResult<OperationDto>> Update(int id, [FromBody] UpdateOperationDto updateDto)
        {
            // Mantém a validação inicial do ModelState
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
                // Ou, para consistência: throw new ValidationException(...)
            }

            // Não precisa de try-catch ou if (updatedOperation == null).
            // O serviço lançará NotFoundException ou ConflictException, e o middleware tratará.
            var updatedOperation = await _operationService.UpdateAsync(id, updateDto);

            // O serviço agora lança exceção se não encontrar, então este null check não é mais necessário
            // if (updatedOperation == null)
            // {
            //     _logger.LogWarning("Operação com ID {Id} não encontrada para atualização.", id);
            //     return NotFound($"Operação com ID {id} não encontrada.");
            // }

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
        [ProducesResponseType(404)] // Documenta a resposta 404 esperada
        [ProducesResponseType(409)] // Documenta a resposta 409 esperada (se implementar validação de dependência)
        [ProducesResponseType(500)] // Mantido para documentação
        public async Task<IActionResult> Delete(int id)
        {
            // Não precisa de try-catch ou if (!success).
            // O serviço lançará NotFoundException ou ConflictException (se implementado), e o middleware tratará.
            // Se o serviço retornar true (já inativo), o NoContent() é retornado.
            var success = await _operationService.DeleteAsync(id);

            // Este check agora é tratado pela exceção no serviço
            // if (!success)
            // {
            //     _logger.LogWarning("Operação com ID {Id} não encontrada para exclusão ou falha ao excluir.", id);
            //     return NotFound($"Operação com ID {id} não encontrada.");
            // }

            return NoContent(); // Retorna 204 se o serviço indicar sucesso (inclusive se já estava inativo)
        }
    }
}
