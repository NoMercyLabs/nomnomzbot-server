// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.Pipeline;

/// <summary>
/// Executes user-defined command pipelines.
///
/// Pipeline JSON format:
/// {
///   "steps": [
///     {
///       "condition": { "type": "user_role", "min_role": "moderator" },
///       "stop_on_match": false,
///       "action": { "type": "send_message", "message": "Hello {user}!" }
///     }
///   ]
/// }
///
/// Limits:
///   - Max 5 concurrent pipelines per channel
///   - Max 5-minute execution timeout per pipeline
///   - Cancelled when channel goes offline
/// </summary>
public sealed class PipelineEngine : IPipelineEngine
{
    private static readonly TimeSpan ExecutionTimeout = TimeSpan.FromMinutes(5);
    private const int MaxConcurrentPerChannel = 5;

    private readonly IChannelRegistry _registry;
    private readonly IEnumerable<ICommandAction> _actions;
    private readonly IEnumerable<ICommandCondition> _conditions;
    private readonly ILogger<PipelineEngine> _logger;

    // Per-channel active count (separate from the CancellationTokenSources in ChannelContext)
    private readonly ConcurrentDictionary<string, int> _activeCount = new();

    public PipelineEngine(
        IChannelRegistry registry,
        IEnumerable<ICommandAction> actions,
        IEnumerable<ICommandCondition> conditions,
        ILogger<PipelineEngine> logger
    )
    {
        _registry = registry;
        _actions = actions;
        _conditions = conditions;
        _logger = logger;
    }

    public int GetActiveCountForChannel(string broadcasterId) =>
        _activeCount.GetValueOrDefault(broadcasterId, 0);

    public async Task CancelAllForChannelAsync(string broadcasterId)
    {
        var ctx = _registry.Get(broadcasterId);
        if (ctx is null)
            return;

        foreach (var (id, cts) in ctx.ActivePipelines)
        {
            try
            {
                await cts.CancelAsync();
            }
            catch
            { /* best-effort */
            }
        }

        _logger.LogInformation(
            "Cancelled all pipelines for channel {BroadcasterId}",
            broadcasterId
        );
    }

    public async Task<PipelineExecutionResult> ExecuteAsync(
        PipelineRequest request,
        CancellationToken ct = default
    )
    {
        var startedAt = DateTimeOffset.UtcNow;

        // Concurrency gate
        var current = _activeCount.AddOrUpdate(request.BroadcasterId, 1, (_, v) => v + 1);
        if (current > MaxConcurrentPerChannel)
        {
            _activeCount.AddOrUpdate(request.BroadcasterId, 0, (_, v) => Math.Max(0, v - 1));
            return new PipelineExecutionResult
            {
                ExecutionId = Guid.NewGuid().ToString("N")[..12],
                Outcome = PipelineOutcome.Failed,
                Duration = TimeSpan.Zero,
                ErrorMessage =
                    $"Channel {request.BroadcasterId} has too many active pipelines ({MaxConcurrentPerChannel} max)",
            };
        }

        // Parse the pipeline definition
        PipelineDefinition? definition;
        try
        {
            definition = JsonSerializer.Deserialize<PipelineDefinition>(
                request.PipelineJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }
        catch (Exception ex)
        {
            _activeCount.AddOrUpdate(request.BroadcasterId, 0, (_, v) => Math.Max(0, v - 1));
            return new PipelineExecutionResult
            {
                ExecutionId = Guid.NewGuid().ToString("N")[..12],
                Outcome = PipelineOutcome.Failed,
                Duration = DateTimeOffset.UtcNow - startedAt,
                ErrorMessage = $"Invalid pipeline JSON: {ex.Message}",
            };
        }

        if (definition is null || definition.Steps.Count == 0)
        {
            _activeCount.AddOrUpdate(request.BroadcasterId, 0, (_, v) => Math.Max(0, v - 1));
            return new PipelineExecutionResult
            {
                ExecutionId = Guid.NewGuid().ToString("N")[..12],
                Outcome = PipelineOutcome.Completed,
                Duration = DateTimeOffset.UtcNow - startedAt,
            };
        }

        // Build execution context
        var execCtx = new PipelineExecutionContext
        {
            BroadcasterId = request.BroadcasterId,
            TriggeredByUserId = request.TriggeredByUserId,
            TriggeredByDisplayName = request.TriggeredByDisplayName,
            MessageId = request.MessageId ?? string.Empty,
            RedemptionId = request.RedemptionId,
            RewardId = request.RewardId,
            RawMessage = request.RawMessage,
            CancellationToken = ct,
        };

        // Seed initial variables
        foreach (var (k, v) in request.InitialVariables)
            execCtx.Variables[k] = v;

        // Register for cancellation via ChannelContext
        using var timeoutCts = new CancellationTokenSource(ExecutionTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        var channelCtx = _registry.Get(request.BroadcasterId);
        if (channelCtx is not null)
            channelCtx.ActivePipelines[execCtx.ExecutionId] = linkedCts;

        try
        {
            return await RunStepsAsync(execCtx, definition, startedAt, linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Pipeline {ExecutionId} timed out in channel {BroadcasterId}",
                execCtx.ExecutionId,
                request.BroadcasterId
            );
            return new PipelineExecutionResult
            {
                ExecutionId = execCtx.ExecutionId,
                Outcome = PipelineOutcome.TimedOut,
                Duration = DateTimeOffset.UtcNow - startedAt,
                StepsExecuted = execCtx.CurrentStepIndex,
                Total = definition.Steps.Count,
                StepLogs = execCtx.StepLogs,
            };
        }
        catch (OperationCanceledException)
        {
            return new PipelineExecutionResult
            {
                ExecutionId = execCtx.ExecutionId,
                Outcome = PipelineOutcome.Cancelled,
                Duration = DateTimeOffset.UtcNow - startedAt,
                StepsExecuted = execCtx.CurrentStepIndex,
                Total = definition.Steps.Count,
                StepLogs = execCtx.StepLogs,
            };
        }
        finally
        {
            channelCtx?.ActivePipelines.TryRemove(execCtx.ExecutionId, out _);
            _activeCount.AddOrUpdate(request.BroadcasterId, 0, (_, v) => Math.Max(0, v - 1));
        }
    }

    // ─── Execution loop ───────────────────────────────────────────────────────

    private async Task<PipelineExecutionResult> RunStepsAsync(
        PipelineExecutionContext ctx,
        PipelineDefinition definition,
        DateTimeOffset startedAt,
        CancellationToken ct
    )
    {
        var executed = 0;
        var skipped = 0;

        for (var i = 0; i < definition.Steps.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            ctx.CurrentStepIndex = i;

            var step = definition.Steps[i];
            var stepStart = DateTimeOffset.UtcNow;

            // Evaluate condition (skip step if condition false)
            if (step.Condition is not null && !EvaluateCondition(ctx, step.Condition))
            {
                skipped++;
                ctx.StepLogs.Add(
                    new StepExecutionLog
                    {
                        StepIndex = i,
                        ActionType = step.Action.Type,
                        Succeeded = true,
                        Duration = DateTimeOffset.UtcNow - stepStart,
                        Output = "Condition not met — step skipped",
                    }
                );
                continue;
            }

            // Execute action
            ActionResult actionResult;
            try
            {
                actionResult = await ExecuteActionAsync(ctx, step.Action, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Pipeline action {ActionType} failed at step {StepIndex}",
                    step.Action.Type,
                    i
                );
                ctx.StepLogs.Add(
                    new StepExecutionLog
                    {
                        StepIndex = i,
                        ActionType = step.Action.Type,
                        Succeeded = false,
                        Duration = DateTimeOffset.UtcNow - stepStart,
                        ErrorMessage = ex.Message,
                    }
                );
                // Continue to next step on action failure (fail-open)
                executed++;
                continue;
            }

            ctx.StepLogs.Add(
                new StepExecutionLog
                {
                    StepIndex = i,
                    ActionType = step.Action.Type,
                    Succeeded = actionResult.Succeeded,
                    Duration = DateTimeOffset.UtcNow - stepStart,
                    Output = actionResult.Output,
                    ErrorMessage = actionResult.ErrorMessage,
                }
            );

            if (actionResult.Succeeded)
                executed++;

            // Check stop flag
            if (ctx.ShouldStop || (step.StopOnMatch && actionResult.Succeeded))
                break;
        }

        return new PipelineExecutionResult
        {
            ExecutionId = ctx.ExecutionId,
            Outcome = PipelineOutcome.Completed,
            Duration = DateTimeOffset.UtcNow - startedAt,
            StepsExecuted = executed,
            StepsSkipped = skipped,
            Total = definition.Steps.Count,
            StepLogs = ctx.StepLogs,
        };
    }

    private bool EvaluateCondition(PipelineExecutionContext ctx, ConditionDefinition condition)
    {
        var evaluator = _conditions.FirstOrDefault(c =>
            string.Equals(c.ConditionType, condition.Type, StringComparison.OrdinalIgnoreCase)
        );

        if (evaluator is null)
        {
            _logger.LogWarning(
                "Unknown condition type '{Type}' — treating as true",
                condition.Type
            );
            return true;
        }

        return evaluator.Evaluate(ctx, condition);
    }

    private async Task<ActionResult> ExecuteActionAsync(
        PipelineExecutionContext ctx,
        ActionDefinition action,
        CancellationToken ct
    )
    {
        var executor = _actions.FirstOrDefault(a =>
            string.Equals(a.ActionType, action.Type, StringComparison.OrdinalIgnoreCase)
        );

        if (executor is null)
        {
            _logger.LogWarning("Unknown action type '{Type}' — skipping", action.Type);
            return ActionResult.Failure($"Unknown action type '{action.Type}'");
        }

        return await executor.ExecuteAsync(ctx, action);
    }
}
