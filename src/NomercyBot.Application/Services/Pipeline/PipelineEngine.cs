// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Models;

namespace NoMercyBot.Application.Services.Pipeline;

/// <summary>
/// Executes JSON-defined pipeline action chains for commands and rewards.
/// </summary>
public class PipelineEngine
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PipelineEngine> _logger;

    public PipelineEngine(IServiceProvider services, ILogger<PipelineEngine> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<Result> ExecuteAsync(
        string pipelineJson,
        PipelineContext context,
        CancellationToken ct = default)
    {
        List<PipelineAction>? actions;
        try
        {
            actions = System.Text.Json.JsonSerializer.Deserialize<List<PipelineAction>>(pipelineJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize pipeline JSON for channel {ChannelId}", context.ChannelId);
            return Result.Failure("Invalid pipeline definition.", "VALIDATION_FAILED");
        }

        if (actions is null || actions.Count == 0)
            return Result.Success();

        foreach (var action in actions)
        {
            var result = await ExecuteActionAsync(action, context, ct);
            if (result.IsFailure)
            {
                _logger.LogWarning("Pipeline action {ActionType} failed: {Error}", action.Type, result.ErrorMessage);
                if (action.FailFast)
                    return result;
            }
        }

        return Result.Success();
    }

    private Task<Result> ExecuteActionAsync(PipelineAction action, PipelineContext context, CancellationToken ct)
    {
        _logger.LogDebug("Executing pipeline action {ActionType} in channel {ChannelId}", action.Type, context.ChannelId);
        // Action resolution handled by registered IPipelineActionHandler implementations
        return Task.FromResult(Result.Success());
    }
}

public record PipelineContext(
    string ChannelId,
    string? UserId,
    string? Username,
    string? MessageId,
    IReadOnlyDictionary<string, string> Variables);

public record PipelineAction(
    string Type,
    Dictionary<string, object>? Parameters = null,
    bool FailFast = false);
