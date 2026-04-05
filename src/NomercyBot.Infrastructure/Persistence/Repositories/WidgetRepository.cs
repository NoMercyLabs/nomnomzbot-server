// SPDX-License-Identifier: AGPL-3.0-or-later
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Repositories;

public class WidgetRepository : GenericRepository<Widget>
{
    public WidgetRepository(AppDbContext db) : base(db) { }

    public Task<List<Widget>> GetByBroadcasterIdAsync(string broadcasterId, CancellationToken ct = default)
        => Set.Where(w => w.BroadcasterId == broadcasterId).OrderBy(w => w.Name).ToListAsync(ct);

    public Task<Widget?> GetByIdAndBroadcasterAsync(string id, string broadcasterId, CancellationToken ct = default)
        => Set.FirstOrDefaultAsync(w => w.Id == id && w.BroadcasterId == broadcasterId, ct);

    public Task<List<Widget>> GetPagedAsync(string broadcasterId, int page, int take, CancellationToken ct = default)
        => Set.Where(w => w.BroadcasterId == broadcasterId).OrderBy(w => w.Name)
              .Skip((page - 1) * take).Take(take).ToListAsync(ct);

    public Task<int> GetCountAsync(string broadcasterId, CancellationToken ct = default)
        => Set.CountAsync(w => w.BroadcasterId == broadcasterId, ct);
}
