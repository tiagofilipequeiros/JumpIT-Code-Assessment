namespace Products.Domain.Entities;

public class Category
{
    // Protected category: products of deleted categories are moved here.
    public const int UncategorizedId = 1;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public List<Product> Products { get; set; } = [];
}
