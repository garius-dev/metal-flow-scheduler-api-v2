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
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OperationTypeDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<OperationTypeDto>>> GetAll()
        {
            try
            {
                var operationTypes = await _operationTypeService.GetAllEnabledAsync();
                return Ok(operationTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os tipos de operação ativos.");
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Obtém um tipo de operação específico pelo seu ID.
        /// </summary>
        /// <param name="id">O ID do tipo de operação.</param>
        /// <returns>O tipo de operação encontrado.</returns>
        /// <response code="200">Retorna o tipo de operação encontrado.</response>
        /// <response code="404">Se o tipo de operação não for encontrado ou estiver inativo.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OperationTypeDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<OperationTypeDto>> GetById(int id)
        {
            try
            {
                var operationType = await _operationTypeService.GetByIdAsync(id);

                if (operationType == null)
                {
                    return NotFound($"Tipo de Operação com ID {id} não encontrado.");
                }

                return Ok(operationType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar tipo de operação com ID {Id}.", id);
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Cria um novo tipo de operação.
        /// </summary>
        /// <param name="createDto">Os dados para criar o novo tipo de operação.</param>
        /// <returns>O tipo de operação criado.</returns>
        /// <response code="201">Retorna o tipo de operação recém-criado.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpPost]
        [ProducesResponseType(typeof(OperationTypeDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<OperationTypeDto>> Create([FromBody] CreateOperationTypeDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdOperationType = await _operationTypeService.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = createdOperationType.Id, version = "1.0" }, createdOperationType);
            }
            catch (Exception ex) when (ex.Message.Contains("Já existe um Tipo de Operação ativo"))
            {
                _logger.LogWarning(ex, "Erro de validação ao criar tipo de operação: {ErrorMessage}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar tipo de operação.");
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Atualiza um tipo de operação existente.
        /// </summary>
        /// <param name="id">O ID do tipo de operação a ser atualizado.</param>
        /// <param name="updateDto">Os novos dados para o tipo de operação.</param>
        /// <returns>O tipo de operação atualizado.</returns>
        /// <response code="200">Retorna o tipo de operação atualizado.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos.</response>
        /// <response code="404">Se o tipo de operação não for encontrado.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OperationTypeDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateOperationTypeDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updatedOperationType = await _operationTypeService.UpdateAsync(id, updateDto);

                if (updatedOperationType == null)
                {
                    return NotFound($"Tipo de Operação com ID {id} não encontrado.");
                }

                return Ok(updatedOperationType);
            }
            catch (Exception ex) when (ex.Message.Contains("Já existe outro Tipo de Operação ativo") ||
                                      ex.Message.Contains("Não é possível atualizar um Tipo de Operação inativo"))
            {
                _logger.LogWarning(ex, "Erro de validação ao atualizar tipo de operação ID {Id}: {ErrorMessage}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar tipo de operação com ID {Id}.", id);
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Desabilita (soft delete) um tipo de operação.
        /// </summary>
        /// <param name="id">O ID do tipo de operação a ser desabilitado.</param>
        /// <returns>Nenhum conteúdo.</returns>
        /// <response code="204">Tipo de operação desabilitado com sucesso.</response>
        /// <response code="404">Se o tipo de operação não for encontrado.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _operationTypeService.DeleteAsync(id);

                if (!success)
                {
                    return NotFound($"Tipo de Operação com ID {id} não encontrado.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar tipo de operação com ID {Id}.", id);
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }
    }
}
