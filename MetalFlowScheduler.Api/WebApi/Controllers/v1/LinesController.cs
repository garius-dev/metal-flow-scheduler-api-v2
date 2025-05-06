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
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<LineDto>), 200)]
        [ProducesResponseType(500)] // Mantido para indicar que 500 é possível, mas tratado globalmente
        public async Task<ActionResult<IEnumerable<LineDto>>> GetAll()
        {
            // Removido try-catch genérico. Exceções serão tratadas por middleware global.
            var lines = await _lineService.GetAllEnabledAsync();
            return Ok(lines);
        }

        /// <summary>
        /// Obtém uma linha de produção específica pelo seu ID.
        /// </summary>
        /// <param name="id">O ID da linha.</param>
        /// <returns>A linha encontrada.</returns>
        /// <response code="200">Retorna a linha encontrada.</response>
        /// <response code="404">Se a linha não for encontrada ou estiver inativa (tratado pelo serviço/middleware).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LineDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)] // Mantido para indicar que 500 é possível, mas tratado globalmente
        public async Task<ActionResult<LineDto>> GetById(int id)
        {
            // Removido try-catch genérico. NotFound será retornado se o serviço retornar null
            // ou se uma exceção de "não encontrado" for lançada pelo serviço e tratada globalmente.
            var line = await _lineService.GetByIdAsync(id);

            if (line == null)
            {
                // O serviço GetByIdAsync já retorna null se não encontrar ou estiver inativo.
                // Se você implementar exceções customizadas no serviço (ex: NotFoundException),
                // este 'if' pode ser removido e o middleware global cuidaria do 404.
                _logger.LogWarning("Linha com ID {Id} não encontrada (ou inativa).", id);
                return NotFound($"Linha com ID {id} não encontrada.");
            }

            return Ok(line);
        }

        /// <summary>
        /// Cria uma nova linha de produção, incluindo suas rotas de workcenter e produtos disponíveis.
        /// </summary>
        /// <param name="createDto">Os dados para criar a nova linha.</param>
        /// <returns>A linha criada.</returns>
        /// <response code="201">Retorna a linha recém-criada.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos (validação do DTO ou do serviço/middleware).</response>
        /// <response code="409">Se houver conflito (ex: nome duplicado) (tratado pelo serviço/middleware).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpPost]
        [ProducesResponseType(typeof(LineDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)] // Adicionado para indicar possível conflito (nome duplicado)
        [ProducesResponseType(500)] // Mantido para indicar que 500 é possível, mas tratado globalmente
        public async Task<ActionResult<LineDto>> Create([FromBody] CreateLineDto createDto)
        {
            // Mantém a validação inicial do ModelState, que é uma boa prática
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Removido try-catch genérico. Exceções de validação, negócio ou DB
            // lançadas pelo serviço serão tratadas por middleware global.
            var createdLine = await _lineService.CreateAsync(createDto);

            // Retorna 201 Created com a localização e o objeto criado
            return CreatedAtAction(nameof(GetById), new { id = createdLine.Id, version = "1.0" }, createdLine);
        }

        /// <summary>
        /// Atualiza uma linha de produção existente, gerenciando suas rotas e produtos disponíveis.
        /// </summary>
        /// <param name="id">O ID da linha a ser atualizada.</param>
        /// <param name="updateDto">Os novos dados para a linha.</param>
        /// <returns>A linha atualizada.</returns>
        /// <response code="200">Retorna a linha atualizada.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos (validação do DTO ou do serviço/middleware).</response>
        /// <response code="404">Se a linha não for encontrada (tratado pelo serviço/middleware).</response>
        /// <response code="409">Se houver conflito (ex: nome duplicado) (tratado pelo serviço/middleware).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(LineDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)] // Adicionado para indicar possível conflito (nome duplicado)
        [ProducesResponseType(500)] // Mantido para indicar que 500 é possível, mas tratado globalmente
        public async Task<ActionResult<LineDto>> Update(int id, [FromBody] UpdateLineDto updateDto)
        {
            // Mantém a validação inicial do ModelState
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Removido try-catch genérico. Exceções de validação, negócio ou DB
            // lançadas pelo serviço serão tratadas por middleware global.
            // O serviço UpdateAsync retornará null se não encontrar a entidade.
            var updatedLine = await _lineService.UpdateAsync(id, updateDto);

            if (updatedLine == null)
            {
                // O serviço UpdateAsync já retorna null se não encontrar.
                // Se você implementar exceções customizadas no serviço (ex: NotFoundException),
                // este 'if' pode ser removido e o middleware global cuidaria do 404.
                _logger.LogWarning("Linha com ID {Id} não encontrada para atualização.", id);
                return NotFound($"Linha com ID {id} não encontrada.");
            }

            return Ok(updatedLine);
        }

        /// <summary>
        /// Desabilita (soft delete) uma linha de produção.
        /// </summary>
        /// <param name="id">O ID da linha a ser desabilitada.</param>
        /// <returns>Nenhum conteúdo.</returns>
        /// <response code="204">Linha desabilitada com sucesso.</response>
        /// <response code="404">Se a linha não for encontrada (tratado pelo serviço/middleware).</response>
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
            var success = await _lineService.DeleteAsync(id);

            if (!success)
            {
                // O serviço DeleteAsync já retorna false se não encontrar.
                // Se você implementar exceções customizadas no serviço (ex: NotFoundException),
                // este 'if' pode ser removido e o middleware global cuidaria do 404.
                _logger.LogWarning("Linha com ID {Id} não encontrada para exclusão ou falha ao excluir.", id);
                return NotFound($"Linha com ID {id} não encontrada.");
            }

            return NoContent();
        }
    }
}
