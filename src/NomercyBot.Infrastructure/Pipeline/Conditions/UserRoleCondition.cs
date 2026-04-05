// SPDX-License-Identifier: AGPL-3.0-or-later
namespace NoMercyBot.Infrastructure.Pipeline.Conditions;

/// <summary>
/// Condition: check if the triggering user has at least a given role.
/// Roles (ascending): viewer, subscriber, vip, moderator, broadcaster
/// Usage: { "type": "user_role", "min_role": "moderator" }
///        { "type": "user_role", "role": "broadcaster" }
/// </summary>
public sealed class UserRoleCondition : ICommandCondition
{
    public string ConditionType => "user_role";

    public bool Evaluate(PipelineExecutionContext ctx, ConditionDefinition condition)
    {
        var requiredRole = condition.Parameters is not null
            ? (GetParam(condition, "min_role") ?? GetParam(condition, "role") ?? "viewer")
            : "viewer";

        var userRole = ctx.Variables.GetValueOrDefault("user.role", "viewer");
        return RoleLevel(userRole) >= RoleLevel(requiredRole);
    }

    private static string? GetParam(ConditionDefinition c, string key)
    {
        if (c.Parameters is null) return null;
        if (!c.Parameters.TryGetValue(key, out var elem)) return null;
        return elem.ValueKind == System.Text.Json.JsonValueKind.String ? elem.GetString() : null;
    }

    private static int RoleLevel(string role) => role.ToLowerInvariant() switch
    {
        "broadcaster" => 5,
        "moderator" or "mod" => 4,
        "vip" => 3,
        "subscriber" or "sub" => 2,
        _ => 1,  // viewer / everyone
    };
}
