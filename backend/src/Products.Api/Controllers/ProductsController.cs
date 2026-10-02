using Products.Application.Dtos.Products;
using Products.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Products.Api.Controllers;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController(ProductService productService) : ControllerBase
{
    /// <summary>All products. Editors and admins can include disabled and uncategorized ones.</summary>
    [HttpGet]
    public Task<List<ProductResponse>> GetAll(bool includeHidden, CancellationToken cancellationToken) =>
        productService.GetAllAsync(includeHidden, cancellationToken);

    /// <summary>Products whose name contains the given text (case-insensitive).</summary>
    [HttpGet("search")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<List<ProductResponse>> Search([FromQuery] string name, bool includeHidden, CancellationToken cancellationToken) =>
        productService.SearchAsync(name, includeHidden, cancellationToken);

    /// <summary>Products with stock between min and max (both inclusive, both optional).</summary>
    [HttpGet("stock-level")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<List<ProductResponse>> GetByStockLevel(int? min, int? max, bool includeHidden, CancellationToken cancellationToken) =>
        productService.GetByStockLevelAsync(min, max, includeHidden, cancellationToken);

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ProductResponse> GetById(int id, CancellationToken cancellationToken) =>
        productService.GetByIdAsync(id, cancellationToken);

    /// <summary>Every version of the product over time (price, stock, ...).</summary>
    [HttpGet("{id:int}/history")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<List<ProductHistoryResponse>> GetHistory(int id, CancellationToken cancellationToken) =>
        productService.GetHistoryAsync(id, cancellationToken);

    /// <summary>Requires the Editor or Admin role.</summary>
    [HttpPost]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request, CancellationToken cancellationToken)
    {
        var product = await productService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>Requires the Editor or Admin role. Send the rowVersion you received; 409 if someone else changed the product.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ProductResponse> Update(int id, UpdateProductRequest request, CancellationToken cancellationToken) =>
        productService.UpdateAsync(id, request, cancellationToken);

    /// <summary>Requires the Admin role.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await productService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Requires the Editor or Admin role.</summary>
    [HttpPost("{id:int}/enable")]
    public Task<ProductResponse> Enable(int id, CancellationToken cancellationToken) =>
        productService.SetActiveAsync(id, isActive: true, cancellationToken);

    /// <summary>Requires the Editor or Admin role.</summary>
    [HttpPost("{id:int}/disable")]
    public Task<ProductResponse> Disable(int id, CancellationToken cancellationToken) =>
        productService.SetActiveAsync(id, isActive: false, cancellationToken);

    [HttpPost("{id:int}/add-to-stock/{quantity:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ProductResponse> AddToStock(int id, int quantity, CancellationToken cancellationToken) =>
        productService.AddStockAsync(id, quantity, cancellationToken);

    [HttpPost("{id:int}/decrement-stock/{quantity:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ProductResponse> DecrementStock(int id, int quantity, CancellationToken cancellationToken) =>
        productService.DecrementStockAsync(id, quantity, cancellationToken);
}
