// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Application.Pipeline;

namespace NomercyBot.Application.Tests.Pipeline;

public class VariableResolverTests
{
    // ─── Resolve(string, IDictionary) ────────────────────────────────────────

    [Fact]
    public void Resolve_EmptyTemplate_ReturnsEmpty()
    {
        string result = VariableResolver.Resolve(string.Empty, new Dictionary<string, string>());
        result.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_NoPlaceholders_ReturnsOriginal()
    {
        string result = VariableResolver.Resolve("Hello world", new Dictionary<string, string>());
        result.Should().Be("Hello world");
    }

    [Fact]
    public void Resolve_KnownVariable_Substitutes()
    {
        Dictionary<string, string> vars = new() { { "user", "Alice" } };
        string result = VariableResolver.Resolve("Hello {{user}}!", vars);

        result.Should().Be("Hello Alice!");
    }

    [Fact]
    public void Resolve_UnknownVariable_LeavesPlaceholderAsEmpty()
    {
        Dictionary<string, string> vars = new();
        string result = VariableResolver.Resolve("Hello {{unknown}}!", vars);

        // VariableResolver replaces unknown vars with empty string
        result.Should().Be("Hello !");
    }

    [Fact]
    public void Resolve_MultipleVariables_SubstitutesAll()
    {
        Dictionary<string, string> vars = new() { { "greeting", "Hello" }, { "name", "Bob" } };
        string result = VariableResolver.Resolve("{{greeting}}, {{name}}!", vars);

        result.Should().Be("Hello, Bob!");
    }

    [Fact]
    public void Resolve_SameVariableTwice_SubstitutesBoth()
    {
        Dictionary<string, string> vars = new() { { "x", "10" } };
        string result = VariableResolver.Resolve("{{x}} + {{x}} = 20", vars);

        result.Should().Be("10 + 10 = 20");
    }

    // ─── ResolveAll ──────────────────────────────────────────────────────────

    [Fact]
    public void ResolveAll_ResolvesVariablesInEachParameter()
    {
        Dictionary<string, object?> parameters = new()
        {
            { "message", "Hello {{user}}" },
            { "channel", "{{channel}}" },
        };
        Dictionary<string, string> vars = new()
        {
            { "user", "Alice" },
            { "channel", "mychannel" },
        };

        IReadOnlyDictionary<string, string> result = VariableResolver.ResolveAll(parameters, vars);

        result["message"].Should().Be("Hello Alice");
        result["channel"].Should().Be("mychannel");
    }

    [Fact]
    public void ResolveAll_NullValue_TreatedAsEmptyString()
    {
        Dictionary<string, object?> parameters = new() { { "key", null } };
        Dictionary<string, string> vars = new();

        IReadOnlyDictionary<string, string> result = VariableResolver.ResolveAll(parameters, vars);

        result["key"].Should().BeEmpty();
    }

    [Fact]
    public void ResolveAll_IntegerValue_ConvertedToString()
    {
        Dictionary<string, object?> parameters = new() { { "count", 42 } };
        Dictionary<string, string> vars = new();

        IReadOnlyDictionary<string, string> result = VariableResolver.ResolveAll(parameters, vars);

        result["count"].Should().Be("42");
    }

    [Fact]
    public void ResolveAll_EmptyParameters_ReturnsEmptyDictionary()
    {
        IReadOnlyDictionary<string, string> result = VariableResolver.ResolveAll(
            new Dictionary<string, object?>(),
            new Dictionary<string, string>()
        );

        result.Should().BeEmpty();
    }
}
