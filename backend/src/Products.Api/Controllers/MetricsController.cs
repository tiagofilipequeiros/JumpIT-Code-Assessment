using Microsoft.AspNetCore.Mvc;
using Products.Application.Dtos.Metrics;
using Products.Application.Services;

namespace Products.Api.Controllers;

[ApiController]
[Route("api/metrics")]
[Produces("application/json")]
public class MetricsController(MetricsService metricsService) : ControllerBase
{
    /// <summary>Product KPIs, units added/removed per day and top products by units removed. Editor or Admin.</summary>
    [HttpGet("products")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public Task<ProductMetricsResponse> GetProductMetrics(CancellationToken cancellationToken, int days = 30) =>
        metricsService.GetProductMetricsAsync(days, cancellationToken);

    /// <summary>Stock level over time for 1 to 5 products, e.g. ?productIds=100000&amp;productIds=100005. Editor or Admin.</summary>
    [HttpGet("products/stock-history")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public Task<List<ProductStockHistoryResponse>> GetStockHistory(
        [FromQuery] int[] productIds, CancellationToken cancellationToken, int days = 30) =>
        metricsService.GetStockHistoryAsync(productIds, days, cancellationToken);

    /// <summary>User KPIs, activity per day, per user and per hour. Admin only (personal activity data).</summary>
    [HttpGet("users")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public Task<UserMetricsResponse> GetUserMetrics(CancellationToken cancellationToken, int days = 30) =>
        metricsService.GetUserMetricsAsync(days, cancellationToken);
}
