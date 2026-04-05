using System.Text.Json.Serialization;

namespace NoMercyBot.Application.DTOs;

/// <summary>
/// Standard response envelope for mutations and single-item responses.
/// Matches the NoMercy media server pattern.
/// </summary>
public sealed class StatusResponseDto<T>
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "ok";

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("args")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object[]? Args { get; set; }
}
