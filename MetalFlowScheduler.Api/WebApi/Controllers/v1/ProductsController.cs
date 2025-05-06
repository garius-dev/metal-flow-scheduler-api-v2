using Asp.Versioning;
using MetalFlowScheduler.Api.Application.Dtos;
using MetalFlowScheduler.Api.Application.Interfaces;
using MetalFlowScheduler.Api.Application.Exceptions; // For explicit documentation of exceptions if needed
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System; // For ArgumentNullException
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MetalFlowScheduler.Api.WebApi.Controllers.v1
{
    /// <summary>
    /// Controller for managing Products.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    // [Authorize(Roles = "Administrator,Manager")] // Example authorization
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductsController"/> class.
        /// </summary>
        /// <param name="productService">The product service.</param>
        /// <param name="logger">The logger.</param>
        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all enabled products.
        /// </summary>
        /// <returns>A list of enabled products.</returns>
        /// <response code="200">Returns the list of enabled products.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProductDto>), 200)]
        public async Task<IActionResult> GetAllEnabled()
        {
            _logger.LogInformation("Attempting to get all enabled products.");
            var products = await _productService.GetAllEnabledAsync();
            _logger.LogInformation("Successfully retrieved all enabled products.");
            return Ok(products);
        }

        /// <summary>
        /// Gets a specific product by its ID.
        /// </summary>
        /// <param name="id">The ID of the product.</param>
        /// <returns>The product if found.</returns>
        /// <response code="200">Returns the requested product.</response>
        /// <response code="404">If the product is not found or not enabled.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProductDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("Attempting to get product with ID: {ProductId}", id);
            var product = await _productService.GetByIdAsync(id);
            // NotFoundException will be handled by middleware
            _logger.LogInformation("Successfully retrieved product with ID: {ProductId}", id);
            return Ok(product);
        }

        /// <summary>
        /// Creates a new product.
        /// </summary>
        /// <param name="createDto">The product creation data.</param>
        /// <returns>The created product.</returns>
        /// <response code="201">Returns the newly created product.</response>
        /// <response code="400">If the input data is invalid.</response>
        /// <response code="409">If a product with the same name already exists and is active, or if related entities are invalid.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ProductDto), 201)]
        [ProducesResponseType(400)] // From ValidationException
        [ProducesResponseType(409)] // From ConflictException
        public async Task<IActionResult> Create([FromBody] CreateProductDto createDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create product request failed due to invalid model state.");
                return BadRequest(ModelState); // Early exit for basic model validation
            }

            _logger.LogInformation("Attempting to create a new product with name: {ProductName}", createDto.Name);
            var createdProduct = await _productService.CreateAsync(createDto);
            _logger.LogInformation("Successfully created product with ID: {ProductId} and name: {ProductName}", createdProduct.Id, createdProduct.Name);
            return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id, version = "1.0" }, createdProduct);
        }

        /// <summary>
        /// Updates an existing product.
        /// </summary>
        /// <param name="id">The ID of the product to update.</param>
        /// <param name="updateDto">The product update data.</param>
        /// <returns>The updated product.</returns>
        /// <response code="200">Returns the updated product.</response>
        /// <response code="400">If the input data is invalid.</response>
        /// <response code="404">If the product to update is not found.</response>
        /// <response code="409">If the update causes a name conflict with another active product, if the product is inactive, or if related entities are invalid.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ProductDto), 200)]
        [ProducesResponseType(400)] // From ValidationException
        [ProducesResponseType(404)] // From NotFoundException
        [ProducesResponseType(409)] // From ConflictException
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Update product request for ID {ProductId} failed due to invalid model state.", id);
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Attempting to update product with ID: {ProductId}", id);
            var updatedProduct = await _productService.UpdateAsync(id, updateDto);
            _logger.LogInformation("Successfully updated product with ID: {ProductId}", id);
            return Ok(updatedProduct);
        }

        /// <summary>
        /// Disables (soft deletes) a product.
        /// </summary>
        /// <param name="id">The ID of the product to disable.</param>
        /// <returns>No content if successful.</returns>
        /// <response code="204">Product disabled successfully.</response>
        /// <response code="404">If the product to disable is not found.</response>
        /// <response code="409">If the product cannot be disabled due to business rule violations (e.g., active dependencies).</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)] // From NotFoundException
        [ProducesResponseType(409)] // From ConflictException
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Attempting to disable product with ID: {ProductId}", id);
            await _productService.DeleteAsync(id);
            _logger.LogInformation("Successfully disabled product with ID: {ProductId}", id);
            return NoContent();
        }
    }
}
