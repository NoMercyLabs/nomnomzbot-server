// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Interfaces;

/// <summary>
/// Abstraction for text-to-speech synthesis providers (Azure, Edge, Google, etc.).
/// </summary>
public interface ITtsProvider
{
    Task<TtsSynthesisResult> SynthesizeAsync(string text, string voiceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TtsVoiceInfo>> GetVoicesAsync(CancellationToken cancellationToken = default);
}

public class TtsSynthesisResult
{
    public required byte[] AudioData { get; init; }
    public required int DurationMs { get; init; }
    public required string Provider { get; init; }
    public required string VoiceId { get; init; }
    public required string ContentHash { get; init; }
}

public class TtsVoiceInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string Locale { get; init; }
    public required string Gender { get; init; }
    public required string Provider { get; init; }
}
