using System.ComponentModel.DataAnnotations;

namespace Products.Application.Dtos.Categories;

// Body of PUT /api/categories/{id}: same fields plus the version the client last saw.
public class UpdateCategoryRequest : CategoryRequest
{
    [Required]
    public byte[] RowVersion { get; init; } = [];
}
