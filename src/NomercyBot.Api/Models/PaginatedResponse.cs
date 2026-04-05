// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Text.Json.Serialization;

namespace NoMercyBot.Api.Models;

public record PaginatedResponse<T>
{
    [JsonPropertyName("data")]
    public IEnumerable<T> Data { get; set; } = [];

    [JsonPropertyName("nextPage")]
    public int? NextPage { get; set; }

    [JsonPropertyName("hasMore")]
    public bool HasMore { get; set; }
}

public class PageRequestDto
{
    public int Page { get; set; } = 1;
    public int Take { get; set; } = 25;
    public string? Sort { get; set; }
    public string? Order { get; set; } = "asc";
    public string? Search { get; set; }
}
