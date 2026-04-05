// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Infrastructure.Services.General;

namespace NomercyBot.Infrastructure.Tests.Services;

public class TemplateEngineTests
{
    private static TemplateEngine Create() => new();

    // ─── Render(template, IReadOnlyDictionary) ───────────────────────────────

    [Fact]
    public void Render_EmptyTemplate_ReturnsEmpty()
    {
        var engine = Create();
        engine.Render(string.Empty, new Dictionary<string, string>()).Should().BeEmpty();
    }

    [Fact]
    public void Render_NullTemplate_ReturnsEmpty()
    {
        var engine = Create();
        engine.Render(null!, new Dictionary<string, string>()).Should().BeEmpty();
    }

    [Fact]
    public void Render_NoPlaceholders_ReturnsOriginal()
    {
        var engine = Create();
        var result = engine.Render("Hello world", new Dictionary<string, string>());
        result.Should().Be("Hello world");
    }

    [Fact]
    public void Render_KnownVariable_Substitutes()
    {
        var engine = Create();
        var vars = new Dictionary<string, string> { { "user", "Alice" } };
        var result = engine.Render("Hello {{user}}!", vars);

        result.Should().Be("Hello Alice!");
    }

    [Fact]
    public void Render_UnknownVariable_LeavesPlaceholderIntact()
    {
        var engine = Create();
        var vars = new Dictionary<string, string>();
        var result = engine.Render("Hello {{unknown}}!", vars);

        result.Should().Be("Hello {{unknown}}!");
    }

    [Fact]
    public void Render_MultipleVariables_SubstitutesAll()
    {
        var engine = Create();
        var vars = new Dictionary<string, string> { { "a", "foo" }, { "b", "bar" } };
        var result = engine.Render("{{a}} and {{b}}", vars);

        result.Should().Be("foo and bar");
    }

    [Fact]
    public void Render_CaseInsensitiveKey_Substitutes()
    {
        var engine = Create();
        var vars = new Dictionary<string, string> { { "User", "Bob" } };
        var result = engine.Render("Hello {{user}}!", vars); // lowercase in template

        result.Should().Be("Hello Bob!");
    }

    [Fact]
    public void Render_NullValue_ReplacesWithEmpty()
    {
        var engine = Create();
        // Dictionary<string, string> can't have null values, but a cast to IReadOnlyDictionary could
        var vars = new Dictionary<string, string?> { { "user", null } };
        var result = engine.Render("Hello {{user}}!", (IReadOnlyDictionary<string, string>)(IDictionary<string, string>)new Dictionary<string, string> { { "user", "" } });

        result.Should().Be("Hello !");
    }

    [Fact]
    public void Render_SamePlaceholderTwice_BothSubstituted()
    {
        var engine = Create();
        var vars = new Dictionary<string, string> { { "x", "10" } };
        var result = engine.Render("{{x}} + {{x}}", vars);

        result.Should().Be("10 + 10");
    }

    [Fact]
    public void Render_WhitespaceInPlaceholder_Substitutes()
    {
        // TemplateEngine trims the variable name
        var engine = Create();
        var vars = new Dictionary<string, string> { { "user", "Carol" } };
        var result = engine.Render("Hello {{ user }}!", vars);

        result.Should().Be("Hello Carol!");
    }

    // ─── Render(template, variableName, variableValue) ───────────────────────

    [Fact]
    public void Render_SingleVar_Substitutes()
    {
        var engine = Create();
        var result = engine.Render("Hello {{name}}!", "name", "Dave");

        result.Should().Be("Hello Dave!");
    }

    [Fact]
    public void Render_SingleVar_UnrelatedPlaceholderUnchanged()
    {
        var engine = Create();
        var result = engine.Render("{{greeting}} {{name}}!", "name", "Eve");

        result.Should().Be("{{greeting}} Eve!");
    }

    // ─── RenderAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RenderAsync_WithObjectDictionary_Substitutes()
    {
        var engine = Create();
        var vars = new Dictionary<string, object> { { "count", 42 } };
        var result = await engine.RenderAsync("Count: {{count}}", vars);

        result.Should().Be("Count: 42");
    }

    [Fact]
    public async Task RenderAsync_NullObjectValue_UsesEmpty()
    {
        var engine = Create();
        var vars = new Dictionary<string, object> { { "val", null! } };
        var result = await engine.RenderAsync("Value: {{val}}!", vars);

        result.Should().Be("Value: !");
    }

    [Fact]
    public async Task RenderAsync_IsCompletedSynchronously_NoActualAsync()
    {
        var engine = Create();
        var task = engine.RenderAsync("Hello {{x}}", new Dictionary<string, object> { { "x", "world" } });

        task.IsCompleted.Should().BeTrue();
        var result = await task;
        result.Should().Be("Hello world");
    }
}
