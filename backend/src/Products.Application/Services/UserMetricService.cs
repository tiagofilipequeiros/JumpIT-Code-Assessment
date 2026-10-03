using Products.Application.Abstractions;
using Products.Domain.Entities;

namespace Products.Application.Services;

// Records user actions in UserMetrics. Rows are saved together with the change they describe.
public class UserMetricService(IUserMetricRepository metrics, TimeProvider clock)
{
    public void Record(int userId, MetricEntity entity, MetricAction action, int? entityId, string? details = null, int? quantity = null)
    {
        metrics.Add(new UserMetric
        {
            UserId = userId,
            Entity = entity,
            Action = action,
            EntityId = entityId,
            Quantity = quantity,
            Details = details is { Length: > UserMetric.DetailsMaxLength } ? details[..UserMetric.DetailsMaxLength] : details,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
        });
    }
}
