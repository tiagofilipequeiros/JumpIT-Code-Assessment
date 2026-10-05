using System.ComponentModel.DataAnnotations;

namespace Products.Application.Dtos.Categories;

// Body of PUT /api/categories/{id}: same fields plus the version the client last saw.
public class UpdateCategoryRequest : CategoryRequest
{
    // Nullable without a default, so a missing value fails [Required] (400) instead of looking like a conflict (409).
    [Required]
    [MinLength(1)]
    public byte[]? RowVersion { get; init; }
}
