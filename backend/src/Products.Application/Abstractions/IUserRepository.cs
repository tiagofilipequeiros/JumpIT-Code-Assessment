using Products.Domain.Entities;

namespace Products.Application.Abstractions;

public interface IUserRepository
{
    Task<List<User>> ListAsync(CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<Dictionary<int, string>> GetNamesAsync(IEnumerable<int> ids, CancellationToken cancellationToken);
}
