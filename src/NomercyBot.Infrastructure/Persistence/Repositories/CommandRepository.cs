// SPDX-License-Identifier: AGPL-3.0-or-later
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Repositories;

public class CommandRepository : GenericRepository<Command>
{
    public CommandRepository(AppDbContext db)
        : base(db) { }

    public Task<List<Command>> GetByBroadcasterIdAsync(
        string broadcasterId,
        bool? enabled = null,
        CancellationToken ct = default
    )
    {
        var q = Set.Where(c => c.BroadcasterId == broadcasterId);
        if (enabled.HasValue)
            q = q.Where(c => c.IsEnabled == enabled.Value);
        return q.OrderBy(c => c.Name).ToListAsync(ct);
    }

    public Task<Command?> GetByNameAsync(
        string broadcasterId,
        string name,
        CancellationToken ct = default
    ) => Set.FirstOrDefaultAsync(c => c.BroadcasterId == broadcasterId && c.Name == name, ct);

    public Task<bool> ExistsByNameAsync(
        string broadcasterId,
        string name,
        CancellationToken ct = default
    ) => Set.AnyAsync(c => c.BroadcasterId == broadcasterId && c.Name == name, ct);

    public Task<List<Command>> SearchAsync(
        string broadcasterId,
        string search,
        CancellationToken ct = default
    ) =>
        Set.Where(c => c.BroadcasterId == broadcasterId && c.Name.Contains(search)).ToListAsync(ct);

    public Task<int> GetPagedCountAsync(
        string broadcasterId,
        string? search,
        CancellationToken ct = default
    )
    {
        var q = Set.Where(c => c.BroadcasterId == broadcasterId);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(c => c.Name.Contains(search));
        return q.CountAsync(ct);
    }

    public Task<List<Command>> GetPagedAsync(
        string broadcasterId,
        int page,
        int take,
        string? search,
        CancellationToken ct = default
    )
    {
        var q = Set.Where(c => c.BroadcasterId == broadcasterId);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(c => c.Name.Contains(search));
        return q.OrderBy(c => c.Name).Skip((page - 1) * take).Take(take).ToListAsync(ct);
    }
}
