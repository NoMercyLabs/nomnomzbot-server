// SPDX-License-Identifier: AGPL-3.0-or-later
namespace NoMercyBot.Domain.Interfaces;

public interface IPipelineEngine
{
    Task<PipelineExecutionResult> ExecuteAsync(PipelineRequest request, CancellationToken ct = default);
    Task CancelAllForChannelAsync(string broadcasterId);
    int GetActiveCountForChannel(string broadcasterId);
}

public class PipelineRequest
{
    public required string BroadcasterId { get; init; }
    public required string PipelineJson { get; init; }
    public required string TriggeredByUserId { get; init; }
    public required string TriggeredByDisplayName { get; init; }
    public string? MessageId { get; init; }
    public string? RedemptionId { get; init; }
    public string? RewardId { get; init; }
    public string RawMessage { get; init; } = "";
    public Dictionary<string, string> InitialVariables { get; init; } = new();
}

public class PipelineExecutionResult
{
    public required string ExecutionId { get; init; }
    public required PipelineOutcome Outcome { get; init; }
    public required TimeSpan Duration { get; init; }
    public int StepsExecuted { get; init; }
    public int StepsSkipped { get; init; }
    public int Total { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<StepExecutionLog> StepLogs { get; init; } = [];
}

public enum PipelineOutcome { Completed, Stopped, Failed, TimedOut, Cancelled }

public class StepExecutionLog
{
    public required int StepIndex { get; init; }
    public required string ActionType { get; init; }
    public required bool Succeeded { get; init; }
    public required TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Output { get; init; }
}
