// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Contracts.Tts;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.Stubs;

public class TtsServiceStub : ITtsService
{
    private readonly ILogger<TtsServiceStub> _logger;

    public TtsServiceStub(ILogger<TtsServiceStub> logger)
    {
        _logger = logger;
    }

    public Task<TtsResult> SynthesizeAsync(string text, string voiceId, CancellationToken ct = default)
    {
        _logger.LogDebug("[STUB] TTS Synthesize: {Text} with {VoiceId}", text, voiceId);
        return Task.FromResult(new TtsResult([], 0, voiceId, "stub"));
    }

    public Task<IReadOnlyList<TtsVoiceInfo>> GetAvailableVoicesAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<TtsVoiceInfo>>([]);
    }
}
