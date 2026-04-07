// SPDX-License-Identifier: AGPL-3.0-or-later
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Application.Pipeline;

public class PipelineContext
{
    public required string ExecutionId { get; init; }
    public required string BroadcasterId { get; init; }
    public required string TriggeredByUserId { get; init; }
    public required string TriggeredByDisplayName { get; init; }
    public string? MessageId { get; init; }
    public string? RedemptionId { get; init; }
    public string? RewardId { get; init; }
    public string RawMessage { get; init; } = "";
    public Dictionary<string, string> Variables { get; } = new(StringComparer.OrdinalIgnoreCase);
    public int CurrentStepIndex { get; set; }
    public List<StepExecutionLog> StepLogs { get; } = [];
    public bool ShouldStop { get; set; }
    public CancellationToken CancellationToken { get; init; }
}
