// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Text.Json.Serialization;
namespace NoMercyBot.Application.Pipeline;

public class PipelineDefinition
{
    [JsonPropertyName("steps")]
    public List<PipelineStepDefinition> Steps { get; set; } = [];

    [JsonPropertyName("stop_on_match")]
    public bool StopOnMatch { get; set; }

    [JsonPropertyName("timeout_seconds")]
    public int TimeoutSeconds { get; set; } = 300;
}

public class PipelineStepDefinition
{
    [JsonPropertyName("action")]
    public required string Action { get; set; }

    [JsonPropertyName("params")]
    public Dictionary<string, object?> Params { get; set; } = new();

    [JsonPropertyName("condition")]
    public ConditionDefinition? Condition { get; set; }
}

public class ConditionDefinition
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("variable")]
    public string? Variable { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("operator")]
    public string Operator { get; set; } = "equals";
}
