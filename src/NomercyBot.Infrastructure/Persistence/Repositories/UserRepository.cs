// SPDX-License-Identifier: AGPL-3.0-or-later
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Repositories;

public class UserRepository : GenericRepository<User>
{
    public UserRepository(AppDbContext db)
        : base(db) { }

    public Task<User?> GetByIdAsync(string userId, CancellationToken ct = default) =>
        Set.Include(u => u.Pronoun).FirstOrDefaultAsync(u => u.Id == userId, ct);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<List<User>> SearchAsync(
        string query,
        int take = 10,
        CancellationToken ct = default
    ) =>
        Set.Where(u => u.Username.Contains(query) || u.DisplayName.Contains(query))
            .Take(take)
            .ToListAsync(ct);
}
