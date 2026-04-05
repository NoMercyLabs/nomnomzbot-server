// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Interfaces;

/// <summary>
/// Executes command/reward pipelines defined as JSON pipeline definitions.
/// </summary>
public interface IPipelineEngine
{
    Task<PipelineResult> ExecuteAsync(string pipelineJson, IDictionary<string, object> context, CancellationToken cancellationToken = default);
}

public class PipelineResult
{
    public required string Outcome { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> StepsExecuted { get; init; } = [];
}
