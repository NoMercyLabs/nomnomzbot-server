// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Infrastructure.Pipeline;
using NoMercyBot.Infrastructure.Pipeline.Conditions;

namespace NomercyBot.Infrastructure.Tests.Pipeline.Conditions;

public class UserRoleConditionTests
{
    private static PipelineExecutionContext BuildCtx(string role)
    {
        var ctx = new PipelineExecutionContext
        {
            BroadcasterId = "chan",
            TriggeredByUserId = "user",
            TriggeredByDisplayName = "User",
            MessageId = "msg",
            RawMessage = "",
        };
        ctx.Variables["user.role"] = role;
        return ctx;
    }

    private static ConditionDefinition MakeCond(string json) =>
        System.Text.Json.JsonSerializer.Deserialize<ConditionDefinition>(json)!;

    [Theory]
    [InlineData("broadcaster", "broadcaster", true)]
    [InlineData("broadcaster", "moderator", true)]
    [InlineData("broadcaster", "vip", true)]
    [InlineData("broadcaster", "subscriber", true)]
    [InlineData("broadcaster", "viewer", true)]
    public void Evaluate_BroadcasterMeetsAllRoles(string userRole, string minRole, bool expected)
    {
        var condition = new UserRoleCondition();
        var ctx = BuildCtx(userRole);
        var def = MakeCond($$$"""{"type":"user_role","min_role":"{{{minRole}}}"}""");

        var result = condition.Evaluate(ctx, def);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("moderator", "broadcaster", false)]
    [InlineData("moderator", "moderator", true)]
    [InlineData("moderator", "vip", true)]
    [InlineData("moderator", "subscriber", true)]
    [InlineData("moderator", "viewer", true)]
    public void Evaluate_ModeratorMeetsModeratorAndBelow(
        string userRole,
        string minRole,
        bool expected
    )
    {
        var condition = new UserRoleCondition();
        var ctx = BuildCtx(userRole);
        var def = MakeCond($$$"""{"type":"user_role","min_role":"{{{minRole}}}"}""");

        var result = condition.Evaluate(ctx, def);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("viewer", "broadcaster", false)]
    [InlineData("viewer", "moderator", false)]
    [InlineData("viewer", "vip", false)]
    [InlineData("viewer", "subscriber", false)]
    [InlineData("viewer", "viewer", true)]
    public void Evaluate_ViewerOnlyMeetsViewer(string userRole, string minRole, bool expected)
    {
        var condition = new UserRoleCondition();
        var ctx = BuildCtx(userRole);
        var def = MakeCond($$$"""{"type":"user_role","min_role":"{{{minRole}}}"}""");

        var result = condition.Evaluate(ctx, def);
        result.Should().Be(expected);
    }

    [Fact]
    public void Evaluate_ModAlias_Accepted()
    {
        var condition = new UserRoleCondition();
        var ctx = BuildCtx("moderator");
        var def = MakeCond("""{"type":"user_role","min_role":"mod"}""");

        var result = condition.Evaluate(ctx, def);
        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_SubAlias_Accepted()
    {
        var condition = new UserRoleCondition();
        var ctx = BuildCtx("subscriber");
        var def = MakeCond("""{"type":"user_role","min_role":"sub"}""");

        var result = condition.Evaluate(ctx, def);
        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_NoRoleVariable_DefaultsToViewer()
    {
        var condition = new UserRoleCondition();
        var ctx = new PipelineExecutionContext
        {
            BroadcasterId = "chan",
            TriggeredByUserId = "user",
            TriggeredByDisplayName = "User",
            MessageId = "msg",
            RawMessage = "",
        };
        // No user.role variable — defaults to "viewer"
        var def = MakeCond("""{"type":"user_role","min_role":"moderator"}""");

        var result = condition.Evaluate(ctx, def);
        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NoParameters_DefaultsToViewer()
    {
        var condition = new UserRoleCondition();
        var ctx = BuildCtx("broadcaster");
        var def = MakeCond("""{"type":"user_role"}"""); // no min_role param

        // Should default min_role to "viewer", any user meets that
        var result = condition.Evaluate(ctx, def);
        result.Should().BeTrue();
    }

    [Fact]
    public void ConditionType_IsUserRole()
    {
        var condition = new UserRoleCondition();
        condition.ConditionType.Should().Be("user_role");
    }
}
