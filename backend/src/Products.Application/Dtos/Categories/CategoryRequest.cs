using System.ComponentModel.DataAnnotations;
using Products.Domain.Entities;

namespace Products.Application.Dtos.Categories;

// Body of POST /api/categories.
public class CategoryRequest
{
    [Required]
    [StringLength(CategoryLimits.NameMaxLength, MinimumLength = CategoryLimits.NameMinLength)]
    public string Name { get; init; } = string.Empty;
}
