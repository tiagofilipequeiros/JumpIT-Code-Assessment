using Backend.Dtos;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public class CategoriesController(CategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public Task<List<CategoryResponse>> GetAll(bool includeHidden, CancellationToken cancellationToken) =>
        categoryService.GetAllAsync(includeHidden, cancellationToken);

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<CategoryResponse> GetById(int id, CancellationToken cancellationToken) =>
        categoryService.GetByIdAsync(id, cancellationToken);

    /// <summary>Requires the Editor or Admin role.</summary>
    [HttpPost]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Create(CategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await categoryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    /// <summary>Requires the Editor or Admin role.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<CategoryResponse> Update(int id, UpdateCategoryRequest request, CancellationToken cancellationToken) =>
        categoryService.UpdateAsync(id, request, cancellationToken);

    /// <summary>Requires the Admin role. Products of the category are moved to Uncategorized.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await categoryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Requires the Editor or Admin role.</summary>
    [HttpPost("{id:int}/enable")]
    public Task<CategoryResponse> Enable(int id, CancellationToken cancellationToken) =>
        categoryService.SetActiveAsync(id, isActive: true, cancellationToken);

    /// <summary>Requires the Editor or Admin role. Hides the category and its products from normal users.</summary>
    [HttpPost("{id:int}/disable")]
    public Task<CategoryResponse> Disable(int id, CancellationToken cancellationToken) =>
        categoryService.SetActiveAsync(id, isActive: false, cancellationToken);
}
