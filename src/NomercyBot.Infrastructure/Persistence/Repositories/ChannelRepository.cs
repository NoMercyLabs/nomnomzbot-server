// SPDX-License-Identifier: AGPL-3.0-or-later
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Repositories;

public class ChannelRepository : GenericRepository<Channel>
{
    public ChannelRepository(AppDbContext db) : base(db) { }

    public Task<Channel?> GetByBroadcasterIdAsync(string broadcasterId, CancellationToken ct = default)
        => Set.Include(c => c.User)
              .FirstOrDefaultAsync(c => c.Id == broadcasterId, ct);

    public Task<List<Channel>> GetEnabledChannelsAsync(CancellationToken ct = default)
        => Set.Where(c => c.Enabled && c.IsOnboarded).ToListAsync(ct);

    public Task<List<Channel>> GetPagedAsync(int page, int take, string? search, CancellationToken ct = default)
    {
        var query = Set.Include(c => c.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search) || c.Id.Contains(search));
        return query.OrderBy(c => c.Name).Skip((page - 1) * take).Take(take).ToListAsync(ct);
    }

    public Task<int> CountAsync(string? search, CancellationToken ct = default)
    {
        var query = Set.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search) || c.Id.Contains(search));
        return query.CountAsync(ct);
    }

    public Task<Channel?> GetByOverlayTokenAsync(string token, CancellationToken ct = default)
        => Set.FirstOrDefaultAsync(c => c.OverlayToken == token, ct);
}
