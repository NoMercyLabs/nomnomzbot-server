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
        var result = VariableResolver.Resolve(string.Empty, new Dictionary<string, string>());
        result.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_NoPlaceholders_ReturnsOriginal()
    {
        var result = VariableResolver.Resolve("Hello world", new Dictionary<string, string>());
        result.Should().Be("Hello world");
    }

    [Fact]
    public void Resolve_KnownVariable_Substitutes()
    {
        var vars = new Dictionary<string, string> { { "user", "Alice" } };
        var result = VariableResolver.Resolve("Hello {{user}}!", vars);

        result.Should().Be("Hello Alice!");
    }

    [Fact]
    public void Resolve_UnknownVariable_LeavesPlaceholderAsEmpty()
    {
        var vars = new Dictionary<string, string>();
        var result = VariableResolver.Resolve("Hello {{unknown}}!", vars);

        // VariableResolver replaces unknown vars with empty string
        result.Should().Be("Hello !");
    }

    [Fact]
    public void Resolve_MultipleVariables_SubstitutesAll()
    {
        var vars = new Dictionary<string, string> { { "greeting", "Hello" }, { "name", "Bob" } };
        var result = VariableResolver.Resolve("{{greeting}}, {{name}}!", vars);

        result.Should().Be("Hello, Bob!");
    }

    [Fact]
    public void Resolve_SameVariableTwice_SubstitutesBoth()
    {
        var vars = new Dictionary<string, string> { { "x", "10" } };
        var result = VariableResolver.Resolve("{{x}} + {{x}} = 20", vars);

        result.Should().Be("10 + 10 = 20");
    }

    // ─── ResolveAll ──────────────────────────────────────────────────────────

    [Fact]
    public void ResolveAll_ResolvesVariablesInEachParameter()
    {
        var parameters = new Dictionary<string, object?>
        {
            { "message", "Hello {{user}}" },
            { "channel", "{{channel}}" },
        };
        var vars = new Dictionary<string, string>
        {
            { "user", "Alice" },
            { "channel", "mychannel" },
        };

        var result = VariableResolver.ResolveAll(parameters, vars);

        result["message"].Should().Be("Hello Alice");
        result["channel"].Should().Be("mychannel");
    }

    [Fact]
    public void ResolveAll_NullValue_TreatedAsEmptyString()
    {
        var parameters = new Dictionary<string, object?> { { "key", null } };
        var vars = new Dictionary<string, string>();

        var result = VariableResolver.ResolveAll(parameters, vars);

        result["key"].Should().BeEmpty();
    }

    [Fact]
    public void ResolveAll_IntegerValue_ConvertedToString()
    {
        var parameters = new Dictionary<string, object?> { { "count", 42 } };
        var vars = new Dictionary<string, string>();

        var result = VariableResolver.ResolveAll(parameters, vars);

        result["count"].Should().Be("42");
    }

    [Fact]
    public void ResolveAll_EmptyParameters_ReturnsEmptyDictionary()
    {
        var result = VariableResolver.ResolveAll(
            new Dictionary<string, object?>(),
            new Dictionary<string, string>()
        );

        result.Should().BeEmpty();
    }
}
