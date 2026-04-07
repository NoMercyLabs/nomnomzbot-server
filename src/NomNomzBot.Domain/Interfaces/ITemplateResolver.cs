// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Interfaces;

/// <summary>
/// Resolves template strings by replacing placeholders with context values.
/// Example: "Thanks for following, {user}!" -> "Thanks for following, Stoney_Eagle!"
/// </summary>
public interface ITemplateResolver
{
    /// <summary>
    /// Resolves a template string using the provided context dictionary and optional channel context.
    /// Pre-seeded variables in <paramref name="seedVariables"/> take precedence over auto-resolved values.
    /// </summary>
    Task<string> ResolveAsync(
        string template,
        IDictionary<string, string> seedVariables,
        string? broadcasterId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Simple synchronous resolve using only the provided variables (no async DB lookups).</summary>
    string Resolve(string template, IDictionary<string, string> variables);
}
