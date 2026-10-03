using Products.Domain.Entities;

namespace Products.Application.Dtos.Metrics;

// GET /api/metrics/users: everything the Users view of the Metrics tab needs, already aggregated.
public record UserMetricsResponse(
    UserKpis Kpis,
    List<ActivityPoint> ActivityPerDay,
    List<UserActivity> PerUser,
    List<HourlyActivity> PerHour);

public record UserKpis(int ActiveUsers, int Logins, int Edits, int StockChanges);

// Actions on one day (UTC), by group. Every day of the period is present.
public record ActivityPoint(DateOnly Date, int Logins, int Edits, int StockChanges);

public record UserActivity(int UserId, string Name, Role Role, int Logins, int Edits, int StockChanges);

// Number of actions in one hour (UTC). The browser converts to local weekday/hour for the heatmap.
public record HourlyActivity(DateTime HourUtc, int Count);
