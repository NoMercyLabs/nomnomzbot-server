using System.Text.RegularExpressions;
using NoMercyBot.Application.Common.Interfaces;

namespace NoMercyBot.Infrastructure.Services.General;

/// <summary>
/// ITemplateEngine implementation that performs simple {{variable}} substitution.
/// Used for command responses, shoutout templates, and notification messages.
/// </summary>
public sealed partial class TemplateEngine : ITemplateEngine
{
    /// <summary>
    /// Replaces all {{variable}} placeholders in the template with values from the provided dictionary.
    /// Unknown variables are left as-is. Variable names are case-insensitive.
    /// </summary>
    public string Render(string template, IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        return TemplatePattern().Replace(template, match =>
        {
            var variableName = match.Groups[1].Value.Trim();

            // Case-insensitive lookup
            foreach (var kvp in variables)
            {
                if (string.Equals(kvp.Key, variableName, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value ?? string.Empty;
                }
            }

            // Unknown variable -- leave placeholder intact
            return match.Value;
        });
    }

    /// <summary>
    /// Renders a template with a single variable substitution.
    /// </summary>
    public string Render(string template, string variableName, string variableValue)
    {
        return Render(template, (IReadOnlyDictionary<string, string>)new Dictionary<string, string> { { variableName, variableValue } });
    }

    /// <summary>
    /// Async render for templates that require data lookups.
    /// Converts all object values to strings synchronously.
    /// </summary>
    public Task<string> RenderAsync(string template, IDictionary<string, object> variables, CancellationToken cancellationToken = default)
    {
        var stringVars = variables.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value?.ToString() ?? string.Empty);
        return Task.FromResult(Render(template, stringVars));
    }

    [GeneratedRegex(@"\{\{(.+?)\}\}")]
    private static partial Regex TemplatePattern();
}
