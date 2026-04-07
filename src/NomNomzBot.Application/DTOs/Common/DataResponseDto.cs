using System.Text.Json.Serialization;

namespace NoMercyBot.Application.DTOs.Common;

/// <summary>
/// Simple data response wrapper for endpoints that return a single data payload.
/// </summary>
public sealed class DataResponseDto<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}
