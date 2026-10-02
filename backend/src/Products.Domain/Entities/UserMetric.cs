namespace Products.Domain.Entities;

// Which table an action was performed on.
public enum MetricEntity
{
    User,
    Product,
    Category,
}

// What was done.
public enum MetricAction
{
    Login,
    Create,
    Update,
    Delete,
    Enable,
    Disable,
    AddStock,
    DecrementStock,
}

// One row per user action: who did what, on which row, and when.
public class UserMetric
{
    public const int DetailsMaxLength = 500;

    public long Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public MetricEntity Entity { get; set; }
    public MetricAction Action { get; set; }
    public int? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}
