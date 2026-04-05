// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.Pipeline;

// ─── Pipeline definition (deserialized from Command.PipelineJson) ─────────────

public sealed class PipelineDefinition
{
    [JsonPropertyName("steps")]
    public List<PipelineStepDefinition> Steps { get; set; } = [];
}

public sealed class PipelineStepDefinition
{
    [JsonPropertyName("condition")]
    public ConditionDefinition? Condition { get; set; }

    [JsonPropertyName("action")]
    public required ActionDefinition Action { get; set; }

    [JsonPropertyName("stop_on_match")]
    public bool StopOnMatch { get; set; }
}

public sealed class ConditionDefinition
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Parameters { get; set; }
}

public sealed class ActionDefinition
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Parameters { get; set; }

    /// <summary>Gets a string parameter value by key.</summary>
    public string? GetString(string key)
    {
        if (Parameters is null) return null;
        if (!Parameters.TryGetValue(key, out var elem)) return null;
        return elem.ValueKind == JsonValueKind.String ? elem.GetString() : elem.ToString();
    }

    /// <summary>Gets an int parameter value by key.</summary>
    public int GetInt(string key, int defaultValue = 0)
    {
        if (Parameters is null) return defaultValue;
        if (!Parameters.TryGetValue(key, out var elem)) return defaultValue;
        return elem.ValueKind == JsonValueKind.Number ? elem.GetInt32() : defaultValue;
    }
}

// ─── Execution context ────────────────────────────────────────────────────────

/// <summary>
/// Mutable per-execution context. Never shared between executions.
/// </summary>
public sealed class PipelineExecutionContext
{
    public string ExecutionId { get; } = Guid.NewGuid().ToString("N")[..12];
    public required string BroadcasterId { get; init; }
    public required string TriggeredByUserId { get; init; }
    public required string TriggeredByDisplayName { get; init; }
    public required string MessageId { get; init; }
    public string? RedemptionId { get; init; }
    public string? RewardId { get; init; }
    public required string RawMessage { get; init; }
    public CancellationToken CancellationToken { get; init; }

    /// <summary>Pipeline-scoped variables. Keys without braces.</summary>
    public Dictionary<string, string> Variables { get; } = new(StringComparer.OrdinalIgnoreCase);

    public int CurrentStepIndex { get; set; }
    public bool ShouldStop { get; set; }

    /// <summary>Per-step execution logs accumulated during the run.</summary>
    public List<StepExecutionLog> StepLogs { get; } = [];
}

// ─── Action result ────────────────────────────────────────────────────────────

public sealed class ActionResult
{
    public bool Succeeded { get; init; }
    public string? Output { get; init; }
    public string? ErrorMessage { get; init; }

    public static ActionResult Success(string? output = null) =>
        new() { Succeeded = true, Output = output };

    public static ActionResult Failure(string error) =>
        new() { Succeeded = false, ErrorMessage = error };
}

// ─── Action interface ─────────────────────────────────────────────────────────

public interface ICommandAction
{
    string ActionType { get; }
    Task<ActionResult> ExecuteAsync(PipelineExecutionContext ctx, ActionDefinition action);
}

// ─── Condition interface ──────────────────────────────────────────────────────

public interface ICommandCondition
{
    string ConditionType { get; }
    bool Evaluate(PipelineExecutionContext ctx, ConditionDefinition condition);
}
