using Products.Domain.Entities;

namespace Products.Application.Abstractions;

public interface IUserMetricRepository
{
    // Saved by IUnitOfWork together with the change it describes.
    void Add(UserMetric metric);
}
