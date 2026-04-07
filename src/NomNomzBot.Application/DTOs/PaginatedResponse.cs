using System.Text.Json.Serialization;

namespace NoMercyBot.Application.DTOs;

/// <summary>
/// Standard response envelope for paginated list responses.
/// Matches the NoMercy media server pattern.
/// </summary>
public sealed class PaginatedResponse<T>
{
    [JsonPropertyName("data")]
    public IEnumerable<T> Data { get; set; } = [];

    [JsonPropertyName("nextPage")]
    public int? NextPage { get; set; }

    [JsonPropertyName("hasMore")]
    public bool HasMore { get; set; }
}
