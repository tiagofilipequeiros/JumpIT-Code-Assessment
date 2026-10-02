using Microsoft.EntityFrameworkCore;
using Products.Application.Abstractions;
using Products.Domain.Entities;
using Products.Infrastructure.Persistence;

namespace Products.Infrastructure.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<List<User>> ListAsync(CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().OrderBy(u => u.Id).ToListAsync(cancellationToken);

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<Dictionary<int, string>> GetNamesAsync(IEnumerable<int> ids, CancellationToken cancellationToken)
    {
        var idList = ids.ToList();
        return db.Users.AsNoTracking().Where(u => idList.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);
    }
}
