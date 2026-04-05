// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Application.Pipeline;
using NoMercyBot.Application.Pipeline.Conditions;

namespace NomercyBot.Application.Tests.Pipeline.Conditions;

public class VariableEqualsConditionTests
{
    private static ActionContext BuildCtx(Dictionary<string, string>? variables = null) =>
        new()
        {
            BroadcasterId = "chan",
            TriggeredByUserId = "user",
            TriggeredByDisplayName = "User",
            Parameters = new Dictionary<string, object?>(),
            Variables = variables ?? new Dictionary<string, string>(),
        };

    private static ConditionDefinition BuildCond(
        string? variable,
        string? value,
        string op = "equals"
    ) =>
        new()
        {
            Type = "variable_equals",
            Variable = variable,
            Value = value,
            Operator = op,
        };

    [Fact]
    public async Task EvaluateAsync_VariableEqualsValue_ReturnsTrue()
    {
        VariableEqualsCondition cond = new();
        ActionContext ctx = BuildCtx(new() { { "x", "42" } });
        ConditionDefinition def = BuildCond("x", "42");

        bool result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_VariableNotEqualsValue_ReturnsFalse()
    {
        VariableEqualsCondition cond = new();
        ActionContext ctx = BuildCtx(new() { { "x", "10" } });
        ConditionDefinition def = BuildCond("x", "42");

        bool result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_VariableNotSet_ReturnsFalse()
    {
        VariableEqualsCondition cond = new();
        ActionContext ctx = BuildCtx(); // no variables
        ConditionDefinition def = BuildCond("missing", "value");

        bool result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_NullVariable_ReturnsFalse()
    {
        VariableEqualsCondition cond = new();
        ActionContext ctx = BuildCtx();
        ConditionDefinition def = BuildCond(null, "value");

        bool result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_CaseInsensitiveComparison_ReturnsTrue()
    {
        VariableEqualsCondition cond = new();
        ActionContext ctx = BuildCtx(new() { { "name", "Alice" } });
        ConditionDefinition def = BuildCond("name", "alice"); // lowercase value

        bool result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_NotEquals_ReturnsTrueWhenDifferent()
    {
        VariableEqualsCondition cond = new();
        ActionContext ctx = BuildCtx(new() { { "x", "10" } });
        ConditionDefinition def = BuildCond("x", "99", "not_equals");

        bool result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_NotEquals_ReturnsFalseWhenSame()
    {
        VariableEqualsCondition cond = new();
        ActionContext ctx = BuildCtx(new() { { "x", "42" } });
        ConditionDefinition def = BuildCond("x", "42", "not_equals");

        bool result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_Contains_ReturnsTrueWhenSubstring()
    {
        VariableEqualsCondition cond = new();
        ActionContext ctx = BuildCtx(new() { { "msg", "hello world" } });
        ConditionDefinition def = BuildCond("msg", "world", "contains");

        bool result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_Contains_ReturnsFalseWhenNotSubstring()
    {
        VariableEqualsCondition cond = new();
        ActionContext ctx = BuildCtx(new() { { "msg", "hello world" } });
        ConditionDefinition def = BuildCond("msg", "xyz", "contains");

        bool result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_StartsWith_ReturnsTrueWhenPrefix()
    {
        VariableEqualsCondition cond = new();
        ActionContext ctx = BuildCtx(new() { { "msg", "hello world" } });
        ConditionDefinition def = BuildCond("msg", "hello", "starts_with");

        bool result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_IsEmpty_ReturnsTrueForEmpty()
    {
        VariableEqualsCondition cond = new();
        ActionContext ctx = BuildCtx(new() { { "x", "" } });
        ConditionDefinition def = BuildCond("x", null, "is_empty");

        bool result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_IsEmpty_ReturnsFalseForNonEmpty()
    {
        VariableEqualsCondition cond = new();
        ActionContext ctx = BuildCtx(new() { { "x", "value" } });
        ConditionDefinition def = BuildCond("x", null, "is_empty");

        bool result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_IsNotEmpty_ReturnsTrueForNonEmpty()
    {
        VariableEqualsCondition cond = new();
        ActionContext ctx = BuildCtx(new() { { "x", "hello" } });
        ConditionDefinition def = BuildCond("x", null, "is_not_empty");

        bool result = await cond.EvaluateAsync(def, ctx);
        result.Should().BeTrue();
    }

    [Fact]
    public void Type_IsVariableEquals()
    {
        VariableEqualsCondition cond = new();
        cond.Type.Should().Be("variable_equals");
    }
}
