namespace Products.Application.Dtos.Metrics;

// GET /api/metrics/products: everything the Products view of the Metrics tab needs, already aggregated.
public record ProductMetricsResponse(
    ProductKpis Kpis,
    List<StockMovementPoint> MovementsPerDay,
    List<ProductUnits> TopRemoved);

// Current state of active products, plus movements in the selected period.
public record ProductKpis(decimal InventoryValue, int OutOfStock, int LowStock, int UnitsAdded, int UnitsRemoved);

// Units added and removed on one day (UTC). Every day of the period is present, with zeros if nothing moved.
public record StockMovementPoint(DateOnly Date, int Added, int Removed);

public record ProductUnits(int ProductId, string Name, int Units);
