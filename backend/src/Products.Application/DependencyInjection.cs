using Microsoft.Extensions.DependencyInjection;
using Products.Application.Services;

namespace Products.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CurrentUser>();
        services.AddScoped<UserMetricService>();
        services.AddScoped<ProductService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<UserService>();
        services.AddScoped<MetricsService>();
        return services;
    }
}
