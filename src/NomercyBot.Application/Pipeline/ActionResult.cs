// SPDX-License-Identifier: AGPL-3.0-or-later
namespace NoMercyBot.Application.Pipeline;

public class ActionResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, string> VariablesSet { get; init; } = new();
    public bool StopPipeline { get; init; }
    public string? Output { get; init; }

    public static ActionResult Ok(string? output = null, Dictionary<string, string>? vars = null)
        => new() { Success = true, Output = output, VariablesSet = vars ?? new() };

    public static ActionResult Fail(string error)
        => new() { Success = false, ErrorMessage = error };

    public static ActionResult Stop(string? output = null)
        => new() { Success = true, StopPipeline = true, Output = output };
}
