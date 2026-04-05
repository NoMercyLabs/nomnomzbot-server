// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Interfaces;

/// <summary>
/// Resolves template strings by replacing placeholders with context values.
/// Example: "Thanks for following, {user}!" -> "Thanks for following, Stoney_Eagle!"
/// </summary>
public interface ITemplateResolver
{
    Task<string> ResolveAsync(string template, IDictionary<string, object> context, CancellationToken cancellationToken = default);
}
