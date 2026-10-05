using System.ComponentModel.DataAnnotations;
using Products.Application.Validation;
using Products.Domain.Entities;

namespace Products.Application.Dtos.Categories;

// Body of POST /api/categories.
public class CategoryRequest
{
    private readonly string _name = string.Empty;

    [Required]
    [StringLength(CategoryLimits.NameMaxLength, MinimumLength = CategoryLimits.NameMinLength)]
    public string Name { get => _name; init => _name = TextInput.Trim(value); }
}
