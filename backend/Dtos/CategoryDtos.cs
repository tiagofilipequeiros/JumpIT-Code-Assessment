using System.ComponentModel.DataAnnotations;
using Backend.Models;

namespace Backend.Dtos;

public record CategoryResponse(
    int Id,
    string Name,
    bool IsActive,
    bool IsProtected,
    int ProductCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    byte[] RowVersion);

// Body of POST /api/categories.
public class CategoryRequest
{
    [Required]
    [StringLength(CategoryLimits.NameMaxLength, MinimumLength = CategoryLimits.NameMinLength)]
    public string Name { get; init; } = string.Empty;
}

// Body of PUT /api/categories/{id}.
public class UpdateCategoryRequest : CategoryRequest
{
    [Required]
    public byte[] RowVersion { get; init; } = [];
}
