using Products.Application.Abstractions;
using Products.Domain.Entities;
using Products.Infrastructure.Persistence;

namespace Products.Infrastructure.Repositories;

public class UserMetricRepository(AppDbContext db) : IUserMetricRepository
{
    public void Add(UserMetric metric) => db.UserMetrics.Add(metric);
}
