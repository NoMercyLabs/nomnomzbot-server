// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Text.RegularExpressions;
namespace NoMercyBot.Application.Pipeline;

public static partial class VariableResolver
{
    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex VariablePattern();

    public static string Resolve(string template, IDictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(template)) return template;
        return VariablePattern().Replace(template, m =>
        {
            var key = m.Groups[1].Value;
            return variables.TryGetValue(key, out var value) ? value : string.Empty;
        });
    }

    public static IReadOnlyDictionary<string, string> ResolveAll(
        IReadOnlyDictionary<string, object?> parameters,
        IDictionary<string, string> variables)
    {
        var result = new Dictionary<string, string>();
        foreach (var (k, v) in parameters)
        {
            var str = v?.ToString() ?? string.Empty;
            result[k] = Resolve(str, variables);
        }
        return result;
    }
}
