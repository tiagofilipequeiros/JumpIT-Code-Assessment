using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Products.Application.Abstractions;
using Products.Infrastructure.Persistence;
using Products.Infrastructure.Repositories;

namespace Products.Infrastructure;

public static class DependencyInjection
{
    // SQL Server error for temporal tables under concurrency: a transaction that began earlier tries to update a row that
    // a later transaction already changed (versions would go back in time). Retrying the whole transaction resolves it.
    private const int TemporalTransactionTimeConflict = 13535;

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            // Retries transient SQL Server errors (e.g. a short network blip or failover) and temporal-table conflicts.
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(
                maxRetryCount: 6,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: [TemporalTransactionTimeConflict])));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserMetricRepository, UserMetricRepository>();
        services.AddScoped<IMetricsRepository, MetricsRepository>();
        services.AddScoped<DemoActivitySeeder>();
        return services;
    }

    // Generates demo activity for the Metrics tab (only when there is no activity yet).
    public static async Task SeedDemoActivityAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DemoActivitySeeder>().SeedAsync(CancellationToken.None);
    }

    // Creates or updates the database to the latest migration.
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }
}
