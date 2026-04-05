// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Application.Pipeline;
using NoMercyBot.Application.Pipeline.Conditions;

namespace NomercyBot.Application.Tests.Pipeline.Conditions;

public class VariableEqualsConditionTests
{
    private static ActionContext BuildCtx(Dictionary<string, string>? variables = null)
        => new()
        {
            BroadcasterId = "chan",
            TriggeredByUserId = "user",
            TriggeredByDisplayName = "User",
            Parameters = new Dictionary<string, object?>(),
            Variables = variables ?? new Dictionary<string, string>()
        };

    private static ConditionDefinition BuildCond(
        string? variable,
        string? value,
        string op = "equals")
        => new() { Type = "variable_equals", Variable = variable, Value = value, Operator = op };

    [Fact]
    public async Task EvaluateAsync_VariableEqualsValue_ReturnsTrue()
    {
        var cond = new VariableEqualsCondition();
        var ctx = BuildCtx(new Dictionary<string, string> { { "x", "42" } });
        var def = BuildCond("x", "42");

        var result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_VariableNotEqualsValue_ReturnsFalse()
    {
        var cond = new VariableEqualsCondition();
        var ctx = BuildCtx(new Dictionary<string, string> { { "x", "10" } });
        var def = BuildCond("x", "42");

        var result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_VariableNotSet_ReturnsFalse()
    {
        var cond = new VariableEqualsCondition();
        var ctx = BuildCtx(); // no variables
        var def = BuildCond("missing", "value");

        var result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_NullVariable_ReturnsFalse()
    {
        var cond = new VariableEqualsCondition();
        var ctx = BuildCtx();
        var def = BuildCond(null, "value");

        var result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_CaseInsensitiveComparison_ReturnsTrue()
    {
        var cond = new VariableEqualsCondition();
        var ctx = BuildCtx(new Dictionary<string, string> { { "name", "Alice" } });
        var def = BuildCond("name", "alice"); // lowercase value

        var result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_NotEquals_ReturnsTrueWhenDifferent()
    {
        var cond = new VariableEqualsCondition();
        var ctx = BuildCtx(new Dictionary<string, string> { { "x", "10" } });
        var def = BuildCond("x", "99", "not_equals");

        var result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_NotEquals_ReturnsFalseWhenSame()
    {
        var cond = new VariableEqualsCondition();
        var ctx = BuildCtx(new Dictionary<string, string> { { "x", "42" } });
        var def = BuildCond("x", "42", "not_equals");

        var result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_Contains_ReturnsTrueWhenSubstring()
    {
        var cond = new VariableEqualsCondition();
        var ctx = BuildCtx(new Dictionary<string, string> { { "msg", "hello world" } });
        var def = BuildCond("msg", "world", "contains");

        var result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_Contains_ReturnsFalseWhenNotSubstring()
    {
        var cond = new VariableEqualsCondition();
        var ctx = BuildCtx(new Dictionary<string, string> { { "msg", "hello world" } });
        var def = BuildCond("msg", "xyz", "contains");

        var result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_StartsWith_ReturnsTrueWhenPrefix()
    {
        var cond = new VariableEqualsCondition();
        var ctx = BuildCtx(new Dictionary<string, string> { { "msg", "hello world" } });
        var def = BuildCond("msg", "hello", "starts_with");

        var result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_IsEmpty_ReturnsTrueForEmpty()
    {
        var cond = new VariableEqualsCondition();
        var ctx = BuildCtx(new Dictionary<string, string> { { "x", "" } });
        var def = BuildCond("x", null, "is_empty");

        var result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_IsEmpty_ReturnsFalseForNonEmpty()
    {
        var cond = new VariableEqualsCondition();
        var ctx = BuildCtx(new Dictionary<string, string> { { "x", "value" } });
        var def = BuildCond("x", null, "is_empty");

        var result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_IsNotEmpty_ReturnsTrueForNonEmpty()
    {
        var cond = new VariableEqualsCondition();
        var ctx = BuildCtx(new Dictionary<string, string> { { "x", "hello" } });
        var def = BuildCond("x", null, "is_not_empty");

        var result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeTrue();
    }

    [Fact]
    public void Type_IsVariableEquals()
    {
        var cond = new VariableEqualsCondition();
        cond.Type.Should().Be("variable_equals");
    }
}
