using Asp.Versioning;
using MetalFlowScheduler.Api.Application.Dtos;
using MetalFlowScheduler.Api.Application.Interfaces;
// TODO: using MetalFlowScheduler.Api.Application.Exceptions; // Para exceções customizadas
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MetalFlowScheduler.Api.WebApi.Controllers.v1
{
    /// <summary>
    /// Controlador para gerenciar Centros de Trabalho (WorkCenters).
    /// </summary>
    [ApiVersion("1.0")] // RG07: Versionamento
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class WorkCentersController : ControllerBase
    {
        private readonly IWorkCenterService _workCenterService;
        private readonly ILogger<WorkCentersController> _logger;

        public WorkCentersController(IWorkCenterService workCenterService, ILogger<WorkCentersController> logger)
        {
            _workCenterService = workCenterService ?? throw new ArgumentNullException(nameof(workCenterService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtém todos os centros de trabalho ativos.
        /// </summary>
        /// <returns>Uma lista de centros de trabalho.</returns>
        /// <response code="200">Retorna a lista de centros de trabalho ativos.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<WorkCenterDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<WorkCenterDto>>> GetAll()
        {
            try
            {
                var workCenters = await _workCenterService.GetAllEnabledAsync();
                return Ok(workCenters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os centros de trabalho ativos.");
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Obtém um centro de trabalho específico pelo seu ID.
        /// </summary>
        /// <param name="id">O ID do centro de trabalho.</param>
        /// <returns>O centro de trabalho encontrado.</returns>
        /// <response code="200">Retorna o centro de trabalho encontrado.</response>
        /// <response code="404">Se o centro de trabalho não for encontrado ou estiver inativo.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(WorkCenterDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<WorkCenterDto>> GetById(int id)
        {
            try
            {
                var workCenter = await _workCenterService.GetByIdAsync(id);

                if (workCenter == null)
                {
                    _logger.LogWarning("Centro de trabalho com ID {Id} não encontrado.", id);
                    return NotFound($"Centro de Trabalho com ID {id} não encontrado.");
                }

                return Ok(workCenter);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar centro de trabalho com ID {Id}.", id);
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Cria um novo centro de trabalho, incluindo a definição de suas rotas de operação.
        /// </summary>
        /// <param name="createDto">Os dados para criar o novo centro de trabalho.</param>
        /// <returns>O centro de trabalho criado.</returns>
        /// <response code="201">Retorna o centro de trabalho recém-criado.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos (ex: nome duplicado, ID relacionado inválido).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpPost]
        [ProducesResponseType(typeof(WorkCenterDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<WorkCenterDto>> Create([FromBody] CreateWorkCenterDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdWorkCenter = await _workCenterService.CreateAsync(createDto);
                // Retorna 201 Created com a localização e o objeto criado
                return CreatedAtAction(nameof(GetById), new { id = createdWorkCenter.Id, version = "1.0" }, createdWorkCenter);
            }

            catch (Exception ex) when (ex.Message.Contains("Já existe um Centro de Trabalho ativo") ||
                                       ex.Message.Contains("inválida ou inativa") ||
                                       ex.Message.Contains("inválidos ou inativos"))
            {
                _logger.LogWarning(ex, "Erro de validação ao criar centro de trabalho: {ErrorMessage}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar centro de trabalho.");
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Atualiza um centro de trabalho existente, gerenciando suas rotas de operação.
        /// </summary>
        /// <param name="id">O ID do centro de trabalho a ser atualizado.</param>
        /// <param name="updateDto">Os novos dados para o centro de trabalho.</param>
        /// <returns>O centro de trabalho atualizado.</returns>
        /// <response code="200">Retorna o centro de trabalho atualizado.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos.</response>
        /// <response code="404">Se o centro de trabalho não for encontrado.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(WorkCenterDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<WorkCenterDto>> Update(int id, [FromBody] UpdateWorkCenterDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updatedWorkCenter = await _workCenterService.UpdateAsync(id, updateDto);

                if (updatedWorkCenter == null)
                {
                    _logger.LogWarning("Centro de trabalho com ID {Id} não encontrado para atualização.", id);
                    return NotFound($"Centro de Trabalho com ID {id} não encontrado.");
                }

                return Ok(updatedWorkCenter);
            }

            catch (Exception ex) when (ex.Message.Contains("Já existe outro Centro de Trabalho ativo") ||
                                      ex.Message.Contains("inválida ou inativa") ||
                                      ex.Message.Contains("inválidos ou inativos") ||
                                      ex.Message.Contains("Não é possível atualizar um Centro de Trabalho inativo"))
            {
                _logger.LogWarning(ex, "Erro de validação ao atualizar centro de trabalho ID {Id}: {ErrorMessage}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar centro de trabalho com ID {Id}.", id);
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Desabilita (soft delete) um centro de trabalho.
        /// </summary>
        /// <param name="id">O ID do centro de trabalho a ser desabilitado.</param>
        /// <returns>Nenhum conteúdo.</returns>
        /// <response code="204">Centro de trabalho desabilitado com sucesso.</response>
        /// <response code="404">Se o centro de trabalho não for encontrado.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _workCenterService.DeleteAsync(id);

                if (!success)
                {
                    _logger.LogWarning("Centro de trabalho com ID {Id} não encontrado para exclusão ou falha ao excluir.", id);
                    return NotFound($"Centro de Trabalho com ID {id} não encontrado.");
                }

                return NoContent();
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar centro de trabalho com ID {Id}.", id);
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }
    }
}
