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
        foreach (ICommandAction a in actions)
            actionRegistry.Register(a);
        foreach (IConditionEvaluator c in conditions)
            conditionRegistry.Register(c);
    }

    public async Task<PipelineExecutionResult> ExecuteAsync(
        PipelineRequest request,
        CancellationToken ct = default
    )
    {
        string executionId = Guid.NewGuid().ToString("N")[..12];
        DateTimeOffset started = DateTimeOffset.UtcNow;

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
            return new()
            {
                ExecutionId = executionId,
                Outcome = PipelineOutcome.Completed,
                Duration = TimeSpan.Zero,
            };

        // Check per-channel limit
        ConcurrentDictionary<string, CancellationTokenSource> channelActive = _active.GetOrAdd(
            request.BroadcasterId,
            _ => new()
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
        TimeSpan timeout = TimeSpan.FromSeconds(
            definition.TimeoutSeconds > 0 ? definition.TimeoutSeconds : 300
        );
        using CancellationTokenSource timeoutCts = new(timeout);
        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        channelActive[executionId] = linkedCts;

        PipelineContext ctx = new()
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
        foreach ((string k, string v) in request.InitialVariables)
            ctx.Variables[k] = v;

        try
        {
            PipelineOutcome outcome = await ExecuteStepsAsync(ctx, definition);
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
        for (int i = 0; i < definition.Steps.Count; i++)
        {
            ctx.CurrentStepIndex = i;
            ctx.CancellationToken.ThrowIfCancellationRequested();

            PipelineStepDefinition step = definition.Steps[i];
            DateTimeOffset stepStart = DateTimeOffset.UtcNow;

            // Evaluate condition
            if (step.Condition != null)
            {
                try
                {
                    IConditionEvaluator? evaluator = _conditionRegistry.GetEvaluator(step.Condition.Type);
                    if (evaluator != null)
                    {
                        ActionContext actionCtx = BuildActionContext(ctx, step);
                        bool condResult = await evaluator.EvaluateAsync(step.Condition, actionCtx);
                        if (!condResult)
                        {
                            ctx.StepLogs.Add(
                                new()
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
            ICommandAction? action = _actionRegistry.GetAction(step.Action);
            if (action == null)
            {
                _logger.LogWarning("Unknown action type '{Action}' at step {i}", step.Action, i);
                ctx.StepLogs.Add(
                    new()
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
            ActionContext actionContext = BuildActionContext(ctx, step);
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
                    new()
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
            foreach ((string k, string v) in result.VariablesSet)
                ctx.Variables[k] = v;

            ctx.StepLogs.Add(
                new()
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
        if (_active.TryGetValue(broadcasterId, out ConcurrentDictionary<string, CancellationTokenSource>? channel))
        {
            foreach ((string _, CancellationTokenSource cts) in channel)
                cts.Cancel();
        }
        return Task.CompletedTask;
    }

    public int GetActiveCountForChannel(string broadcasterId) =>
        _active.TryGetValue(broadcasterId, out ConcurrentDictionary<string, CancellationTokenSource>? ch) ? ch.Count : 0;

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
