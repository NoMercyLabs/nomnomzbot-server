// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Infrastructure.BackgroundServices.Queues;

public interface IBackgroundTaskQueue
{
    ValueTask QueueAsync(
        Func<CancellationToken, ValueTask> workItem,
        CancellationToken ct = default
    );
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken ct);
}
