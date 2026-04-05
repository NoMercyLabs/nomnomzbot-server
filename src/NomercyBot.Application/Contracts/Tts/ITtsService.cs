// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Application.Contracts.Tts;

public interface ITtsService
{
    Task<TtsResult> SynthesizeAsync(string text, string voiceId, CancellationToken ct = default);
    Task<IReadOnlyList<TtsVoiceInfo>> GetAvailableVoicesAsync(CancellationToken ct = default);
}

public record TtsResult(byte[] AudioData, int DurationMs, string VoiceId, string Provider);
public record TtsVoiceInfo(string Id, string Name, string DisplayName, string Locale, string Gender, string Provider);
