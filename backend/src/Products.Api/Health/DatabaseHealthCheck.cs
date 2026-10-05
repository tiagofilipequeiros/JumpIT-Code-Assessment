using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Products.Infrastructure.Persistence;

namespace Products.Api.Health;

// Healthy when the database answers "SELECT 1" within a few seconds. Uses its own short-timeout connection:
// through EF the retry policy and the default 15 s connect timeout would make a failing check take far too long.
// WaitAsync is a hard limit on top, because the SQL client can exceed its own connect timeout (e.g. host gone).
public class DatabaseHealthCheck(AppDbContext db) : IHealthCheck
{
    private const int TimeoutSeconds = 3;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        var connectionString = new SqlConnectionStringBuilder(db.Database.GetConnectionString())
        {
            ConnectTimeout = TimeoutSeconds,
            ConnectRetryCount = 0,
        }.ConnectionString;

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).WaitAsync(Timeout, cancellationToken);
            await using var command = new SqlCommand("SELECT 1", connection) { CommandTimeout = TimeoutSeconds };
            await command.ExecuteScalarAsync(cancellationToken).WaitAsync(Timeout, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is SqlException or InvalidOperationException or TimeoutException)
        {
            return HealthCheckResult.Unhealthy("The database did not answer.", exception);
        }
    }
}
