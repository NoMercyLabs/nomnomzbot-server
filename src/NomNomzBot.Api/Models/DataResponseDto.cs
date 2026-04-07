// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Text.Json.Serialization;

namespace NoMercyBot.Api.Models;

public record DataResponseDto<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}
