using Asp.Versioning;
using MetalFlowScheduler.Api.Application.Dtos;
using MetalFlowScheduler.Api.Application.Interfaces;
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
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OperationDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<OperationDto>>> GetAll()
        {
            try
            {
                var operations = await _operationService.GetAllEnabledAsync();
                return Ok(operations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todas as operações ativas.");
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Obtém uma operação específica pelo seu ID.
        /// </summary>
        /// <param name="id">O ID da operação.</param>
        /// <returns>A operação encontrada.</returns>
        /// <response code="200">Retorna a operação encontrada.</response>
        /// <response code="404">Se a operação não for encontrada ou estiver inativa.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OperationDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<OperationDto>> GetById(int id)
        {
            try
            {
                var operation = await _operationService.GetByIdAsync(id);

                if (operation == null)
                {
                    return NotFound($"Operação com ID {id} não encontrada.");
                }

                return Ok(operation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar operação com ID {Id}.", id);
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Cria uma nova operação.
        /// </summary>
        /// <param name="createDto">Os dados para criar a nova operação.</param>
        /// <returns>A operação criada.</returns>
        /// <response code="201">Retorna a operação recém-criada.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpPost]
        [ProducesResponseType(typeof(OperationDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<OperationDto>> Create([FromBody] CreateOperationDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdOperation = await _operationService.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = createdOperation.Id, version = "1.0" }, createdOperation);
            }
            catch (Exception ex) when (ex.Message.Contains("Já existe uma Operação ativa") ||
                                       ex.Message.Contains("inválido ou inativo"))
            {
                _logger.LogWarning(ex, "Erro de validação ao criar operação: {ErrorMessage}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar operação.");
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Atualiza uma operação existente.
        /// </summary>
        /// <param name="id">O ID da operação a ser atualizada.</param>
        /// <param name="updateDto">Os novos dados para a operação.</param>
        /// <returns>O objeto atualizado.</returns>
        /// <response code="200">Retorna a operação atualizada.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos.</response>
        /// <response code="404">Se a operação não for encontrada.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OperationDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<OperationDto>> Update(int id, [FromBody] UpdateOperationDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updatedOperation = await _operationService.UpdateAsync(id, updateDto);

                if (updatedOperation == null)
                {
                    return NotFound($"Operação com ID {id} não encontrada.");
                }

                return Ok(updatedOperation);
            }
            catch (Exception ex) when (ex.Message.Contains("Já existe outra Operação ativa") ||
                                      ex.Message.Contains("inválido ou inativo") ||
                                      ex.Message.Contains("Não é possível atualizar uma Operação inativa"))
            {
                _logger.LogWarning(ex, "Erro de validação ao atualizar operação ID {Id}: {ErrorMessage}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar operação com ID {Id}.", id);
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Desabilita (soft delete) uma operação.
        /// </summary>
        /// <param name="id">O ID da operação a ser desabilitada.</param>
        /// <returns>Nenhum conteúdo.</returns>
        /// <response code="204">Operação desabilitada com sucesso.</response>
        /// <response code="404">Se a operação não for encontrada.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _operationService.DeleteAsync(id);

                if (!success)
                {
                    return NotFound($"Operação com ID {id} não encontrada.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar operação com ID {Id}.", id);
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }
    }
}
