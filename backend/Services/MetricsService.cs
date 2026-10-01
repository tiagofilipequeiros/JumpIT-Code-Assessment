using Backend.Data;
using Backend.Models;

namespace Backend.Services;

// Records user actions in UserMetrics. Rows are saved together with the change they describe.
public class MetricsService(AppDbContext db, TimeProvider clock)
{
    public void Record(int userId, MetricEntity entity, MetricAction action, int? entityId, string? details = null)
    {
        db.UserMetrics.Add(new UserMetric
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
