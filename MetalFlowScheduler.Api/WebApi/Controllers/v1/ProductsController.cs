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
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProductDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
        {
            try
            {
                var products = await _productService.GetAllEnabledAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os produtos ativos.");
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Obtém um produto específico pelo seu ID.
        /// </summary>
        /// <param name="id">O ID do produto.</param>
        /// <returns>O produto encontrado.</returns>
        /// <response code="200">Retorna o produto encontrado.</response>
        /// <response code="404">Se o produto não for encontrado ou estiver inativo.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProductDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            try
            {
                var product = await _productService.GetByIdAsync(id);

                if (product == null)
                {
                    _logger.LogWarning("Produto com ID {Id} não encontrado.", id);
                    return NotFound($"Produto com ID {id} não encontrado."); 
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar produto com ID {Id}.", id);
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Cria um novo produto, incluindo opcionalmente suas rotas de operação.
        /// </summary>
        /// <param name="createDto">Os dados para criar o novo produto.</param>
        /// <returns>O produto criado.</returns>
        /// <response code="201">Retorna o produto recém-criado.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos (ex: nome duplicado, ID de tipo de operação inválido).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ProductDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdProduct = await _productService.CreateAsync(createDto);
                // Retorna 201 Created com a localização e o objeto criado
                return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id, version = "1.0" }, createdProduct);
            }
            
            catch (Exception ex) when (ex.Message.Contains("Já existe um Produto ativo") ||
                                       ex.Message.Contains("inválidos ou inativos")) // Tratamento exemplo
            {
                _logger.LogWarning(ex, "Erro de validação ao criar produto: {ErrorMessage}", ex.Message);
                return BadRequest(ex.Message); // RG02: Mensagem em PT-BR
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar produto.");
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Atualiza um produto existente, gerenciando opcionalmente suas rotas de operação.
        /// </summary>
        /// <param name="id">O ID do produto a ser atualizado.</param>
        /// <param name="updateDto">Os novos dados para o produto.</param>
        /// <returns>O produto atualizado.</returns>
        /// <response code="200">Retorna o produto atualizado.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos.</response>
        /// <response code="404">Se o produto não for encontrado.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ProductDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updatedProduct = await _productService.UpdateAsync(id, updateDto);

                if (updatedProduct == null)
                {
                    _logger.LogWarning("Produto com ID {Id} não encontrado para atualização.", id);
                    return NotFound($"Produto com ID {id} não encontrado.");
                }

                return Ok(updatedProduct);
            }

            catch (Exception ex) when (ex.Message.Contains("Já existe outro Produto ativo") ||
                                      ex.Message.Contains("inválidos ou inativos") ||
                                      ex.Message.Contains("Não é possível atualizar um Produto inativo"))
            {
                _logger.LogWarning(ex, "Erro de validação ao atualizar produto ID {Id}: {ErrorMessage}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar produto com ID {Id}.", id);
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }

        /// <summary>
        /// Desabilita (soft delete) um produto.
        /// </summary>
        /// <param name="id">O ID do produto a ser desabilitado.</param>
        /// <returns>Nenhum conteúdo.</returns>
        /// <response code="204">Produto desabilitado com sucesso.</response>
        /// <response code="404">Se o produto não for encontrado.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _productService.DeleteAsync(id);

                if (!success)
                {
                    _logger.LogWarning("Produto com ID {Id} não encontrado para exclusão ou falha ao excluir.", id);
                    return NotFound($"Produto com ID {id} não encontrado.");
                }

                return NoContent();
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar produto com ID {Id}.", id);
                return StatusCode(500, "Ocorreu um erro interno ao processar a sua solicitação.");
            }
        }
    }
}
