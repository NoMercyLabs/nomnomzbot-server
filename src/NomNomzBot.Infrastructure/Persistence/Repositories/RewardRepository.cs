// SPDX-License-Identifier: AGPL-3.0-or-later
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Repositories;

public class RewardRepository : GenericRepository<Reward>
{
    public RewardRepository(AppDbContext db)
        : base(db) { }

    public Task<List<Reward>> GetByBroadcasterIdAsync(
        string broadcasterId,
        CancellationToken ct = default
    ) => Set.Where(r => r.BroadcasterId == broadcasterId).OrderBy(r => r.Title).ToListAsync(ct);

    public Task<Reward?> GetByIdAndBroadcasterAsync(
        Guid id,
        string broadcasterId,
        CancellationToken ct = default
    ) => Set.FirstOrDefaultAsync(r => r.Id == id && r.BroadcasterId == broadcasterId, ct);

    public Task<List<Reward>> GetPagedAsync(
        string broadcasterId,
        int page,
        int take,
        CancellationToken ct = default
    ) =>
        Set.Where(r => r.BroadcasterId == broadcasterId)
            .OrderBy(r => r.Title)
            .Skip((page - 1) * take)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> GetCountAsync(string broadcasterId, CancellationToken ct = default) =>
        Set.CountAsync(r => r.BroadcasterId == broadcasterId, ct);
}
