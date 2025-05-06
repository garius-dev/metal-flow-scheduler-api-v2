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
    /// Controlador para gerenciar Produtos.
    /// </summary>
    [ApiVersion("1.0")] // RG07: Versionamento
    [Route("api/v{version:apiVersion}/[controller]")] // RG07: Rota versionada
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtém todos os produtos ativos.
        /// </summary>
        /// <returns>Uma lista de produtos.</returns>
        /// <response code="200">Retorna a lista de produtos ativos.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProductDto>), 200)]
        [ProducesResponseType(500)] // Mantido para indicar que 500 é possível, mas tratado globalmente
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
        {
            // Removido try-catch genérico. Exceções serão tratadas por middleware global.
            var products = await _productService.GetAllEnabledAsync();
            return Ok(products);
        }

        /// <summary>
        /// Obtém um produto específico pelo seu ID.
        /// </summary>
        /// <param name="id">O ID do produto.</param>
        /// <returns>O produto encontrado.</returns>
        /// <response code="200">Retorna o produto encontrado.</response>
        /// <response code="404">Se o produto não for encontrado ou estiver inativo (tratado pelo serviço/middleware).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProductDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)] // Mantido para indicar que 500 é possível, mas tratado globalmente
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            // Removido try-catch genérico. NotFound será retornado se o serviço retornar null
            // ou se uma exceção de "não encontrado" for lançada pelo serviço e tratada globalmente.
            var product = await _productService.GetByIdAsync(id);

            if (product == null)
            {
                // O serviço GetByIdAsync já retorna null se não encontrar ou estiver inativo.
                // Se você implementar exceções customizadas no serviço (ex: NotFoundException),
                // este 'if' pode ser removido e o middleware global cuidaria do 404.
                _logger.LogWarning("Produto com ID {Id} não encontrado (ou inativo).", id);
                return NotFound($"Produto com ID {id} não encontrado.");
            }

            return Ok(product);
        }

        /// <summary>
        /// Cria um novo produto, incluindo opcionalmente suas rotas de operação.
        /// </summary>
        /// <param name="createDto">Os dados para criar o novo produto.</param>
        /// <returns>O produto criado.</returns>
        /// <response code="201">Retorna o produto recém-criado.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos (validação do DTO ou do serviço/middleware).</response>
        /// <response code="409">Se houver conflito (ex: nome duplicado) (tratado pelo serviço/middleware).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpPost]
        [ProducesResponseType(typeof(ProductDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)] // Adicionado para indicar possível conflito (nome duplicado)
        [ProducesResponseType(500)] // Mantido para indicar que 500 é possível, mas tratado globalmente
        public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto createDto)
        {
            // Mantém a validação inicial do ModelState
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Removido try-catch genérico e tratamento específico de exceções de validação/negócio.
            // Exceções lançadas pelo serviço (validação de IDs, nome duplicado, etc.)
            // serão tratadas por middleware global, retornando 400 ou 409.
            var createdProduct = await _productService.CreateAsync(createDto);

            // Retorna 201 Created com a localização e o objeto criado
            return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id, version = "1.0" }, createdProduct);
        }

        /// <summary>
        /// Atualiza um produto existente, gerenciando opcionalmente suas rotas de operação.
        /// </summary>
        /// <param name="id">O ID do produto a ser atualizado.</param>
        /// <param name="updateDto">Os novos dados para o produto.</param>
        /// <returns>O produto atualizado.</returns>
        /// <response code="200">Retorna o produto atualizado.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos (validação do DTO ou do serviço/middleware).</response>
        /// <response code="404">Se o produto não for encontrado (tratado pelo serviço/middleware).</response>
        /// <response code="409">Se houver conflito (ex: nome duplicado) (tratado pelo serviço/middleware).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ProductDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)] // Adicionado para indicar possível conflito (nome duplicado)
        [ProducesResponseType(500)] // Mantido para indicar que 500 é possível, mas tratado globalmente
        public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductDto updateDto)
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
            var updatedProduct = await _productService.UpdateAsync(id, updateDto);

            if (updatedProduct == null)
            {
                // O serviço UpdateAsync já retorna null se não encontrar.
                // Se você implementar exceções customizadas no serviço (ex: NotFoundException),
                // este 'if' pode ser removido e o middleware global cuidaria do 404.
                _logger.LogWarning("Produto com ID {Id} não encontrado para atualização.", id);
                return NotFound($"Produto com ID {id} não encontrado.");
            }

            return Ok(updatedProduct);
        }

        /// <summary>
        /// Desabilita (soft delete) um produto.
        /// </summary>
        /// <param name="id">O ID do produto a ser desabilitado.</param>
        /// <returns>Nenhum conteúdo.</returns>
        /// <response code="204">Produto desabilitado com sucesso.</response>
        /// <response code="404">Se o produto não for encontrado (tratado pelo serviço/middleware).</response>
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
            var success = await _productService.DeleteAsync(id);

            if (!success)
            {
                // O serviço DeleteAsync já retorna false se não encontrar.
                // Se você implementar exceções customizadas no serviço (ex: NotFoundException),
                // este 'if' pode ser removido e o middleware global cuidaria do 404.
                _logger.LogWarning("Produto com ID {Id} não encontrado para exclusão ou falha ao excluir.", id);
                return NotFound($"Produto com ID {id} não encontrado.");
            }

            return NoContent();
        }
    }
}
