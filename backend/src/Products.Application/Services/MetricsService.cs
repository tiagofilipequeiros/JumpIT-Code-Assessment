using Products.Application.Abstractions;
using Products.Domain.Entities;

namespace Products.Application.Services;

// Records user actions in UserMetrics. Rows are saved together with the change they describe.
public class MetricsService(IUserMetricRepository metrics, TimeProvider clock)
{
    public void Record(int userId, MetricEntity entity, MetricAction action, int? entityId, string? details = null)
    {
        metrics.Add(new UserMetric
        {
            UserId = userId,
            Entity = entity,
            Action = action,
            EntityId = entityId,
            Details = details is { Length: > UserMetric.DetailsMaxLength } ? details[..UserMetric.DetailsMaxLength] : details,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
        });
    }
}
