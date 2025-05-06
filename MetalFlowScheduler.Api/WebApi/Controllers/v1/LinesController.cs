// Arquivo: WebApi/Controllers/v1/LinesController.cs
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
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente pelo middleware).</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<LineDto>), 200)]
        [ProducesResponseType(500)] // Mantido para documentação
        public async Task<ActionResult<IEnumerable<LineDto>>> GetAll()
        {
            // Não precisa de try-catch. Exceções do serviço (incluindo 500) serão tratadas pelo middleware.
            var lines = await _lineService.GetAllEnabledAsync();
            return Ok(lines);
        }

        /// <summary>
        /// Obtém uma linha de produção específica pelo seu ID.
        /// </summary>
        /// <param name="id">O ID da linha.</param>
        /// <returns>A linha encontrada.</returns>
        /// <response code="200">Retorna a linha encontrada.</response>
        /// <response code="404">Se a linha não for encontrada ou estiver inativa (tratado pelo middleware via NotFoundException).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente pelo middleware).</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LineDto), 200)]
        [ProducesResponseType(404)] // Documenta a resposta 404 esperada
        [ProducesResponseType(500)] // Mantido para documentação
        public async Task<ActionResult<LineDto>> GetById(int id)
        {
            // Não precisa de try-catch ou if (line == null).
            // O serviço lançará NotFoundException se não encontrar ou estiver inativa,
            // e o middleware converterá para 404.
            var line = await _lineService.GetByIdAsync(id);
            return Ok(line);
        }

        /// <summary>
        /// Cria uma nova linha de produção, incluindo suas rotas de workcenter e produtos disponíveis.
        /// </summary>
        /// <param name="createDto">Os dados para criar a nova linha.</param>
        /// <returns>A linha criada.</returns>
        /// <response code="201">Retorna a linha recém-criada.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos (validação do DTO ou tratado pelo middleware via ValidationException).</response>
        /// <response code="409">Se houver conflito (ex: nome duplicado) (tratado pelo middleware via ConflictException).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente pelo middleware).</response>
        [HttpPost]
        [ProducesResponseType(typeof(LineDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)] // Documenta a resposta 409 esperada
        [ProducesResponseType(500)] // Mantido para documentação
        public async Task<ActionResult<LineDto>> Create([FromBody] CreateLineDto createDto)
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
        /// <response code="400">Se os dados fornecidos forem inválidos (validação do DTO ou tratado pelo middleware).</response>
        /// <response code="404">Se a linha não for encontrada (tratado pelo middleware via NotFoundException).</response>
        /// <response code="409">Se houver conflito (ex: nome duplicado ou item inativo) (tratado pelo middleware via ConflictException).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente pelo middleware).</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(LineDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)] // Documenta a resposta 404 esperada
        [ProducesResponseType(409)] // Documenta a resposta 409 esperada
        [ProducesResponseType(500)] // Mantido para documentação
        public async Task<ActionResult<LineDto>> Update(int id, [FromBody] UpdateLineDto updateDto)
        {
            // Mantém a validação inicial do ModelState
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
                // Ou, para consistência: throw new ValidationException(...)
            }

            // Não precisa de try-catch ou if (updatedLine == null).
            // O serviço lançará NotFoundException ou ConflictException, e o middleware tratará.
            var updatedLine = await _lineService.UpdateAsync(id, updateDto);

            // O serviço agora lança exceção se não encontrar, então este null check não é mais necessário
            // if (updatedLine == null)
            // {
            //     _logger.LogWarning("Linha com ID {Id} não encontrada para atualização.", id);
            //     return NotFound($"Linha com ID {id} não encontrada.");
            // }

            return Ok(updatedLine);
        }

        /// <summary>
        /// Desabilita (soft delete) uma linha de produção.
        /// </summary>
        /// <param name="id">O ID da linha a ser desabilitada.</param>
        /// <returns>Nenhum conteúdo.</returns>
        /// <response code="204">Linha desabilitada com sucesso.</response>
        /// <response code="404">Se a linha não for encontrada (tratado pelo middleware via NotFoundException).</response>
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
            var success = await _lineService.DeleteAsync(id);

            // Este check agora é tratado pela exceção no serviço
            // if (!success)
            // {
            //     _logger.LogWarning("Linha com ID {Id} não encontrada para exclusão ou falha ao excluir.", id);
            //     return NotFound($"Linha com ID {id} não encontrada.");
            // }

            return NoContent(); // Retorna 204 se o serviço indicar sucesso (inclusive se já estava inativo)
        }
    }
}
