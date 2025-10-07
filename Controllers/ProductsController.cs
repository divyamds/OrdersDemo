using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Api.Data;
using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly InMemoryRepository _repo;

        public ProductsController(InMemoryRepository repo)
        {
            _repo = repo;
        }

        // GET: /api/products
        [HttpGet]
        public async Task<IActionResult> List()
        {
            var products = await _repo.ListProducts();
            return Ok(products);
        }

        // GET: /api/products/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _repo.GetProduct(id);
            if (product == null)
                return NotFound();

            Response.Headers.ETag = product.ConcurrencyToken.ToString();
            return Ok(product);
        }

        // POST: /api/products
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] ProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = new Product(0, dto.Name, dto.Price, 0, 1);
            var created = await _repo.AddProduct(product);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: /api/products/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductDto dto, [FromHeader(Name = "If-Match")] int? token)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _repo.GetProduct(id);
            if (existing == null)
                return NotFound();

            if (token == null || token != existing.ConcurrencyToken)
                return StatusCode(412, "Version mismatch");

            var updated = existing with { Name = dto.Name, Price = dto.Price };
            var success = await _repo.UpdateProduct(updated, token.Value);

            if (!success)
                return StatusCode(412, "Version mismatch");

            return Ok(await _repo.GetProduct(id));
        }

        // DELETE: /api/products/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id, [FromHeader(Name = "If-Match")] int? token)
        {
            var existing = await _repo.GetProduct(id);
            if (existing == null)
                return NotFound();

            if (token == null || token != existing.ConcurrencyToken)
                return StatusCode(412, "Version mismatch");

            var deleted = _repo.DeleteProduct(id);
            if (!deleted)
                return StatusCode(500, "Failed to delete");

            return NoContent();
        }
    }

    // DTO for validation
    public record ProductDto(
        [Required, StringLength(100)] string Name,
        [Range(0.01, double.MaxValue)] decimal Price
    );
}
