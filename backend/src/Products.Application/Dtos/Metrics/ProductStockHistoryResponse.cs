namespace Products.Application.Dtos.Metrics;

// GET /api/metrics/products/stock-history: stock level over time for the chosen products (from the temporal history).
public record ProductStockHistoryResponse(int ProductId, string Name, List<StockPoint> Points);

// Stock from this moment until the next point (a step line).
public record StockPoint(DateTime Time, int Stock);
