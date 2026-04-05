// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Threading.Channels;

namespace NoMercyBot.Infrastructure.BackgroundServices.Queues;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

    public BackgroundTaskQueue(int capacity = 100)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
        };
        _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
    }

    public ValueTask QueueAsync(
        Func<CancellationToken, ValueTask> workItem,
        CancellationToken ct = default
    ) => _queue.Writer.WriteAsync(workItem, ct);

    public ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken ct) =>
        _queue.Reader.ReadAsync(ct);
}
