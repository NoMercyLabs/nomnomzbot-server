// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Contracts.Music;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.Pipeline.Actions;

/// <summary>
/// Song-request action: searches for the query and adds the best match to the queue.
///
/// Parameters:
///   query — search query (required). Supports {variable} substitution.
///
/// Usage example:
///   { "type": "song_request", "query": "{args}" }
/// </summary>
public sealed class SongRequestAction : ICommandAction
{
    private readonly IMusicService _music;
    private readonly IChatProvider _chat;
    private readonly ILogger<SongRequestAction> _logger;

    public string ActionType => "song_request";

    public SongRequestAction(
        IMusicService music,
        IChatProvider chat,
        ILogger<SongRequestAction> logger
    )
    {
        _music = music;
        _chat = chat;
        _logger = logger;
    }

    public async Task<ActionResult> ExecuteAsync(
        PipelineExecutionContext ctx,
        ActionDefinition action
    )
    {
        var query = ResolveParam(action.GetString("query") ?? string.Empty, ctx.Variables);
        if (string.IsNullOrWhiteSpace(query))
            return ActionResult.Failure("song_request requires a non-empty 'query'");

        var results = await _music.SearchAsync(ctx.BroadcasterId, query, 1, ctx.CancellationToken);
        if (results.Count == 0)
        {
            await _chat.SendMessageAsync(
                ctx.BroadcasterId,
                $"@{ctx.TriggeredByDisplayName} No tracks found for \"{query}\".",
                ctx.CancellationToken
            );
            return ActionResult.Failure($"no tracks found for query: {query}");
        }

        var track = results[0];
        var added = await _music.AddToQueueAsync(
            ctx.BroadcasterId,
            track.Uri,
            ctx.TriggeredByDisplayName,
            ctx.CancellationToken
        );

        if (!added)
            return ActionResult.Failure("failed to add track to queue");

        await _chat.SendMessageAsync(
            ctx.BroadcasterId,
            $"@{ctx.TriggeredByDisplayName} Added to queue: {track.Name} by {track.Artist}",
            ctx.CancellationToken
        );
        return ActionResult.Success($"queued: {track.Name}");
    }

    private static string ResolveParam(string value, Dictionary<string, string> vars)
    {
        if (value.StartsWith('{') && value.EndsWith('}'))
            vars.TryGetValue(value[1..^1], out value!);
        return value ?? string.Empty;
    }
}

/// <summary>
/// Skip action: skips the current track.
///
/// Usage example:
///   { "type": "song_skip" }
/// </summary>
public sealed class SongSkipAction : ICommandAction
{
    private readonly IMusicService _music;
    private readonly IChatProvider _chat;
    private readonly ILogger<SongSkipAction> _logger;

    public string ActionType => "song_skip";

    public SongSkipAction(IMusicService music, IChatProvider chat, ILogger<SongSkipAction> logger)
    {
        _music = music;
        _chat = chat;
        _logger = logger;
    }

    public async Task<ActionResult> ExecuteAsync(
        PipelineExecutionContext ctx,
        ActionDefinition action
    )
    {
        var skipped = await _music.SkipAsync(ctx.BroadcasterId, ctx.CancellationToken);
        if (!skipped)
            return ActionResult.Failure("skip failed");

        await _chat.SendMessageAsync(
            ctx.BroadcasterId,
            "Skipped to the next track.",
            ctx.CancellationToken
        );
        return ActionResult.Success("skipped");
    }
}

/// <summary>
/// Now-playing action: posts the current song to chat.
///
/// Usage example:
///   { "type": "song_current" }
/// </summary>
public sealed class SongCurrentAction : ICommandAction
{
    private readonly IMusicService _music;
    private readonly IChatProvider _chat;
    private readonly ILogger<SongCurrentAction> _logger;

    public string ActionType => "song_current";

    public SongCurrentAction(
        IMusicService music,
        IChatProvider chat,
        ILogger<SongCurrentAction> logger
    )
    {
        _music = music;
        _chat = chat;
        _logger = logger;
    }

    public async Task<ActionResult> ExecuteAsync(
        PipelineExecutionContext ctx,
        ActionDefinition action
    )
    {
        var now = await _music.GetNowPlayingAsync(ctx.BroadcasterId, ctx.CancellationToken);
        if (now is null || string.IsNullOrWhiteSpace(now.TrackName))
        {
            await _chat.SendMessageAsync(
                ctx.BroadcasterId,
                "Nothing is playing right now.",
                ctx.CancellationToken
            );
            return ActionResult.Success("nothing playing");
        }

        var msg = $"Now playing: {now.TrackName} by {now.Artist}";
        if (now.RequestedBy is not null)
            msg += $" (requested by {now.RequestedBy})";

        await _chat.SendMessageAsync(ctx.BroadcasterId, msg, ctx.CancellationToken);
        return ActionResult.Success(msg);
    }
}

/// <summary>
/// Queue action: posts the upcoming queue to chat.
///
/// Parameters:
///   max — maximum number of tracks to show (default: 5).
///
/// Usage example:
///   { "type": "song_queue", "max": 5 }
/// </summary>
public sealed class SongQueueAction : ICommandAction
{
    private readonly IMusicService _music;
    private readonly IChatProvider _chat;
    private readonly ILogger<SongQueueAction> _logger;

    public string ActionType => "song_queue";

    public SongQueueAction(IMusicService music, IChatProvider chat, ILogger<SongQueueAction> logger)
    {
        _music = music;
        _chat = chat;
        _logger = logger;
    }

    public async Task<ActionResult> ExecuteAsync(
        PipelineExecutionContext ctx,
        ActionDefinition action
    )
    {
        var max = action.GetInt("max", 5);
        var queue = await _music.GetQueueAsync(ctx.BroadcasterId, ctx.CancellationToken);

        if (queue.Queue.Count == 0)
        {
            await _chat.SendMessageAsync(
                ctx.BroadcasterId,
                "The queue is empty.",
                ctx.CancellationToken
            );
            return ActionResult.Success("queue empty");
        }

        var entries = queue
            .Queue.Take(max)
            .Select(
                (t, i) =>
                    t.RequestedBy is not null
                        ? $"{i + 1}. {t.TrackName} by {t.Artist} ({t.RequestedBy})"
                        : $"{i + 1}. {t.TrackName} by {t.Artist}"
            );

        await _chat.SendMessageAsync(
            ctx.BroadcasterId,
            "Queue: " + string.Join(" | ", entries),
            ctx.CancellationToken
        );
        return ActionResult.Success($"showed {queue.Queue.Count} tracks");
    }
}

/// <summary>
/// Volume action: sets the playback volume.
///
/// Parameters:
///   volume — integer 0-100 (required). Supports {variable} substitution.
///
/// Usage example:
///   { "type": "song_volume", "volume": 50 }
/// </summary>
public sealed class SongVolumeAction : ICommandAction
{
    private readonly IMusicService _music;
    private readonly IChatProvider _chat;
    private readonly ILogger<SongVolumeAction> _logger;

    public string ActionType => "song_volume";

    public SongVolumeAction(
        IMusicService music,
        IChatProvider chat,
        ILogger<SongVolumeAction> logger
    )
    {
        _music = music;
        _chat = chat;
        _logger = logger;
    }

    public async Task<ActionResult> ExecuteAsync(
        PipelineExecutionContext ctx,
        ActionDefinition action
    )
    {
        var volumeStr = action.GetString("volume");
        int volume;

        if (volumeStr is not null && volumeStr.StartsWith('{') && volumeStr.EndsWith('}'))
        {
            ctx.Variables.TryGetValue(volumeStr[1..^1], out var resolved);
            if (!int.TryParse(resolved, out volume))
                return ActionResult.Failure("song_volume: 'volume' could not be parsed as integer");
        }
        else
        {
            volume = action.GetInt("volume", -1);
        }

        if (volume is < 0 or > 100)
            return ActionResult.Failure("song_volume: 'volume' must be between 0 and 100");

        var set = await _music.SetVolumeAsync(ctx.BroadcasterId, volume, ctx.CancellationToken);
        if (!set)
            return ActionResult.Failure("song_volume: failed to set volume");

        await _chat.SendMessageAsync(
            ctx.BroadcasterId,
            $"Volume set to {volume}%.",
            ctx.CancellationToken
        );
        return ActionResult.Success($"volume set to {volume}");
    }
}
