// SPDX-License-Identifier: AGPL-3.0-or-later
namespace NoMercyBot.Application.Pipeline;

public class ActionContext
{
    public required string BroadcasterId { get; init; }
    public required string TriggeredByUserId { get; init; }
    public required string TriggeredByDisplayName { get; init; }
    public string? MessageId { get; init; }
    public string? RedemptionId { get; init; }
    public string? RewardId { get; init; }
    public required IReadOnlyDictionary<string, object?> Parameters { get; init; }
    public required IDictionary<string, string> Variables { get; init; }
    public CancellationToken CancellationToken { get; init; }
}
