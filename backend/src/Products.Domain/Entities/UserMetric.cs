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

    // Units added or removed, for AddStock / DecrementStock (always positive; the action gives the direction).
    public int? Quantity { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}

// How actions are grouped in the metrics.
public static class MetricActionGroups
{
    public static readonly MetricAction[] Edits =
        [MetricAction.Create, MetricAction.Update, MetricAction.Delete, MetricAction.Enable, MetricAction.Disable];

    public static readonly MetricAction[] StockChanges = [MetricAction.AddStock, MetricAction.DecrementStock];
}
