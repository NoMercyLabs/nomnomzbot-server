// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Pipeline;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Application.Services.Pipeline;

public class PipelineEngine : IPipelineEngine
{
    private readonly ICommandActionRegistry _actionRegistry;
    private readonly IConditionEvaluatorRegistry _conditionRegistry;
    private readonly ILogger<PipelineEngine> _logger;

    // Per-channel active pipeline CTSes: broadcasterId -> executionId -> CTS
    private readonly ConcurrentDictionary<
        string,
        ConcurrentDictionary<string, CancellationTokenSource>
    > _active = new();

    // Per-channel concurrency limit
    private const int MaxConcurrentPerChannel = 5;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
    };

    public PipelineEngine(
        ICommandActionRegistry actionRegistry,
        IConditionEvaluatorRegistry conditionRegistry,
        IEnumerable<ICommandAction> actions,
        IEnumerable<IConditionEvaluator> conditions,
        ILogger<PipelineEngine> logger
    )
    {
        _actionRegistry = actionRegistry;
        _conditionRegistry = conditionRegistry;
        _logger = logger;
        foreach (var a in actions)
            actionRegistry.Register(a);
        foreach (var c in conditions)
            conditionRegistry.Register(c);
    }

    public async Task<PipelineExecutionResult> ExecuteAsync(
        PipelineRequest request,
        CancellationToken ct = default
    )
    {
        var executionId = Guid.NewGuid().ToString("N")[..12];
        var started = DateTimeOffset.UtcNow;

        // Parse pipeline definition
        PipelineDefinition? definition;
        try
        {
            definition = JsonSerializer.Deserialize<PipelineDefinition>(
                request.PipelineJson,
                JsonOpts
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to parse pipeline JSON for {BroadcasterId}",
                request.BroadcasterId
            );
            return Fail(executionId, "Invalid pipeline JSON", started);
        }

        if (definition == null || definition.Steps.Count == 0)
            return new PipelineExecutionResult
            {
                ExecutionId = executionId,
                Outcome = PipelineOutcome.Completed,
                Duration = TimeSpan.Zero,
            };

        // Check per-channel limit
        var channelActive = _active.GetOrAdd(
            request.BroadcasterId,
            _ => new ConcurrentDictionary<string, CancellationTokenSource>()
        );
        if (channelActive.Count >= MaxConcurrentPerChannel)
        {
            _logger.LogWarning(
                "Channel {BroadcasterId} exceeded pipeline concurrency limit",
                request.BroadcasterId
            );
            return Fail(executionId, "Pipeline concurrency limit reached", started);
        }

        // Setup timeout + link caller's CT
        var timeout = TimeSpan.FromSeconds(
            definition.TimeoutSeconds > 0 ? definition.TimeoutSeconds : 300
        );
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        channelActive[executionId] = linkedCts;

        var ctx = new PipelineContext
        {
            ExecutionId = executionId,
            BroadcasterId = request.BroadcasterId,
            TriggeredByUserId = request.TriggeredByUserId,
            TriggeredByDisplayName = request.TriggeredByDisplayName,
            MessageId = request.MessageId,
            RedemptionId = request.RedemptionId,
            RewardId = request.RewardId,
            RawMessage = request.RawMessage,
            CancellationToken = linkedCts.Token,
        };

        // Seed built-in variables
        ctx.Variables["user"] = request.TriggeredByDisplayName;
        ctx.Variables["user_id"] = request.TriggeredByUserId;
        ctx.Variables["channel"] = request.BroadcasterId;
        ctx.Variables["random"] = Random.Shared.Next(1, 101).ToString();
        foreach (var (k, v) in request.InitialVariables)
            ctx.Variables[k] = v;

        try
        {
            var outcome = await ExecuteStepsAsync(ctx, definition);
            return BuildResult(executionId, outcome, ctx, definition.Steps.Count, started);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return BuildResult(
                executionId,
                PipelineOutcome.TimedOut,
                ctx,
                definition.Steps.Count,
                started
            );
        }
        catch (OperationCanceledException)
        {
            return BuildResult(
                executionId,
                PipelineOutcome.Cancelled,
                ctx,
                definition.Steps.Count,
                started
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in pipeline {ExecutionId}", executionId);
            return Fail(executionId, ex.Message, started);
        }
        finally
        {
            channelActive.TryRemove(executionId, out _);
        }
    }

    private async Task<PipelineOutcome> ExecuteStepsAsync(
        PipelineContext ctx,
        PipelineDefinition definition
    )
    {
        for (var i = 0; i < definition.Steps.Count; i++)
        {
            ctx.CurrentStepIndex = i;
            ctx.CancellationToken.ThrowIfCancellationRequested();

            var step = definition.Steps[i];
            var stepStart = DateTimeOffset.UtcNow;

            // Evaluate condition
            if (step.Condition != null)
            {
                try
                {
                    var evaluator = _conditionRegistry.GetEvaluator(step.Condition.Type);
                    if (evaluator != null)
                    {
                        var actionCtx = BuildActionContext(ctx, step);
                        var condResult = await evaluator.EvaluateAsync(step.Condition, actionCtx);
                        if (!condResult)
                        {
                            ctx.StepLogs.Add(
                                new StepExecutionLog
                                {
                                    StepIndex = i,
                                    ActionType = step.Action,
                                    Succeeded = true,
                                    Duration = DateTimeOffset.UtcNow - stepStart,
                                    Output = "skipped (condition false)",
                                }
                            );
                            continue; // skip this step
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Condition evaluation failed at step {i}, skipping", i);
                    continue;
                }
            }

            // Get action handler
            var action = _actionRegistry.GetAction(step.Action);
            if (action == null)
            {
                _logger.LogWarning("Unknown action type '{Action}' at step {i}", step.Action, i);
                ctx.StepLogs.Add(
                    new StepExecutionLog
                    {
                        StepIndex = i,
                        ActionType = step.Action,
                        Succeeded = false,
                        Duration = DateTimeOffset.UtcNow - stepStart,
                        ErrorMessage = $"Unknown action: {step.Action}",
                    }
                );
                return PipelineOutcome.Failed;
            }

            // Execute action
            var actionContext = BuildActionContext(ctx, step);
            ActionResult result;
            try
            {
                result = await action.ExecuteAsync(actionContext);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Action {Action} threw at step {i}", step.Action, i);
                ctx.StepLogs.Add(
                    new StepExecutionLog
                    {
                        StepIndex = i,
                        ActionType = step.Action,
                        Succeeded = false,
                        Duration = DateTimeOffset.UtcNow - stepStart,
                        ErrorMessage = ex.Message,
                    }
                );
                return PipelineOutcome.Failed;
            }

            // Apply output variables
            foreach (var (k, v) in result.VariablesSet)
                ctx.Variables[k] = v;

            ctx.StepLogs.Add(
                new StepExecutionLog
                {
                    StepIndex = i,
                    ActionType = step.Action,
                    Succeeded = result.Success,
                    Duration = DateTimeOffset.UtcNow - stepStart,
                    ErrorMessage = result.ErrorMessage,
                    Output = result.Output,
                }
            );

            if (!result.Success)
                return PipelineOutcome.Failed;
            if (result.StopPipeline || ctx.ShouldStop)
                return PipelineOutcome.Stopped;
        }

        return PipelineOutcome.Completed;
    }

    private static ActionContext BuildActionContext(
        PipelineContext ctx,
        PipelineStepDefinition step
    ) =>
        new()
        {
            BroadcasterId = ctx.BroadcasterId,
            TriggeredByUserId = ctx.TriggeredByUserId,
            TriggeredByDisplayName = ctx.TriggeredByDisplayName,
            MessageId = ctx.MessageId,
            RedemptionId = ctx.RedemptionId,
            RewardId = ctx.RewardId,
            Parameters = step.Params,
            Variables = ctx.Variables,
            CancellationToken = ctx.CancellationToken,
        };

    public Task CancelAllForChannelAsync(string broadcasterId)
    {
        if (_active.TryGetValue(broadcasterId, out var channel))
        {
            foreach (var (_, cts) in channel)
                cts.Cancel();
        }
        return Task.CompletedTask;
    }

    public int GetActiveCountForChannel(string broadcasterId) =>
        _active.TryGetValue(broadcasterId, out var ch) ? ch.Count : 0;

    private static PipelineExecutionResult Fail(string id, string error, DateTimeOffset started) =>
        new()
        {
            ExecutionId = id,
            Outcome = PipelineOutcome.Failed,
            ErrorMessage = error,
            Duration = DateTimeOffset.UtcNow - started,
        };

    private static PipelineExecutionResult BuildResult(
        string id,
        PipelineOutcome outcome,
        PipelineContext ctx,
        int total,
        DateTimeOffset started
    ) =>
        new()
        {
            ExecutionId = id,
            Outcome = outcome,
            Duration = DateTimeOffset.UtcNow - started,
            StepsExecuted = ctx.StepLogs.Count(l => l.Output != "skipped (condition false)"),
            StepsSkipped = ctx.StepLogs.Count(l => l.Output == "skipped (condition false)"),
            Total = total,
            StepLogs = ctx.StepLogs,
        };
}
