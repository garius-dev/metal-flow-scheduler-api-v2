using Asp.Versioning;
using MetalFlowScheduler.Api.Application.Dtos;
using MetalFlowScheduler.Api.Application.Interfaces;
// using MetalFlowScheduler.Api.Application.Exceptions; // Para exceções customizadas (opcional)
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MetalFlowScheduler.Api.WebApi.Controllers.v1
{
    /// <summary>
    /// Controlador para gerenciar Linhas de Produção (Lines).
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class LinesController : ControllerBase
    {
        private readonly ILineService _lineService;
        private readonly ILogger<LinesController> _logger;

        public LinesController(ILineService lineService, ILogger<LinesController> logger)
        {
            _lineService = lineService ?? throw new ArgumentNullException(nameof(lineService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtém todas as linhas de produção ativas.
        /// </summary>
        /// <returns>Uma lista de linhas de produção.</returns>
        /// <response code="200">Retorna a lista de linhas ativas.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<LineDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<LineDto>>> GetAll()
        {
            try
            {
                var lines = await _lineService.GetAllEnabledAsync();
                return Ok(lines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todas as linhas ativas.");
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Obtém uma linha de produção específica pelo seu ID.
        /// </summary>
        /// <param name="id">O ID da linha.</param>
        /// <returns>A linha encontrada.</returns>
        /// <response code="200">Retorna a linha encontrada.</response>
        /// <response code="404">Se a linha não for encontrada ou estiver inativa.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LineDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<LineDto>> GetById(int id)
        {
            try
            {
                var line = await _lineService.GetByIdAsync(id);

                if (line == null)
                {
                    return NotFound($"Linha com ID {id} não encontrada.");
                }

                return Ok(line);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar linha com ID {Id}.", id);
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Cria uma nova linha de produção, incluindo suas rotas de workcenter e produtos disponíveis.
        /// </summary>
        /// <param name="createDto">Os dados para criar a nova linha.</param>
        /// <returns>A linha criada.</returns>
        /// <response code="201">Retorna a linha recém-criada.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpPost]
        [ProducesResponseType(typeof(LineDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<LineDto>> Create([FromBody] CreateLineDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdLine = await _lineService.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = createdLine.Id, version = "1.0" }, createdLine);
            }
            catch (Exception ex) when (ex.Message.Contains("Já existe uma Linha ativa") ||
                                       ex.Message.Contains("inválido ou inativo") ||
                                       ex.Message.Contains("não pode ser vazia"))
            {
                _logger.LogWarning(ex, "Erro de validação ao criar linha: {ErrorMessage}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar linha.");
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Atualiza uma linha de produção existente, gerenciando suas rotas e produtos disponíveis.
        /// </summary>
        /// <param name="id">O ID da linha a ser atualizada.</param>
        /// <param name="updateDto">Os novos dados para a linha.</param>
        /// <returns>A linha atualizada.</returns>
        /// <response code="200">Retorna a linha atualizada.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos.</response>
        /// <response code="404">Se a linha não for encontrada.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(LineDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<LineDto>> Update(int id, [FromBody] UpdateLineDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updatedLine = await _lineService.UpdateAsync(id, updateDto);

                if (updatedLine == null)
                {
                    return NotFound($"Linha com ID {id} não encontrada.");
                }

                return Ok(updatedLine);
            }
            catch (Exception ex) when (ex.Message.Contains("Já existe outra Linha ativa") ||
                                      ex.Message.Contains("inválido ou inativo") ||
                                      ex.Message.Contains("não pode ser vazia") ||
                                      ex.Message.Contains("Não é possível atualizar uma Linha inativa"))
            {
                _logger.LogWarning(ex, "Erro de validação ao atualizar linha ID {Id}: {ErrorMessage}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar linha com ID {Id}.", id);
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Desabilita (soft delete) uma linha de produção.
        /// </summary>
        /// <param name="id">O ID da linha a ser desabilitada.</param>
        /// <returns>Nenhum conteúdo.</returns>
        /// <response code="204">Linha desabilitada com sucesso.</response>
        /// <response code="404">Se a linha não for encontrada.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _lineService.DeleteAsync(id);

                if (!success)
                {
                    return NotFound($"Linha com ID {id} não encontrada.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar linha com ID {Id}.", id);
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }
    }
}
