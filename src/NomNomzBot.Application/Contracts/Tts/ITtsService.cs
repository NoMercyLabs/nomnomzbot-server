// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Application.Contracts.Tts;

public interface ITtsService
{
    Task<TtsResult> SynthesizeAsync(string text, string voiceId, CancellationToken ct = default);
    Task<IReadOnlyList<TtsVoiceInfo>> GetAvailableVoicesAsync(CancellationToken ct = default);
}

public record TtsResult(byte[] AudioData, int DurationMs, string VoiceId, string Provider);
