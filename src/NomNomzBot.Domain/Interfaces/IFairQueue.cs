// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Interfaces;

/// <summary>
/// Generic fair queue interface that ensures equitable processing across
/// multiple sources (e.g., per-user song request fairness).
/// </summary>
public interface IFairQueue<T>
{
    /// <summary>Enqueues an item with an associated owner key for fairness.</summary>
    void Enqueue(string ownerKey, T item);

    /// <summary>Dequeues the next item using fair scheduling across owners.</summary>
    T? Dequeue();

    /// <summary>Peeks at the next item without removing it.</summary>
    T? Peek();

    /// <summary>Gets the total number of items in the queue.</summary>
    int Count { get; }

    /// <summary>Checks if the queue is empty.</summary>
    bool IsEmpty { get; }

    /// <summary>Removes all items from the queue.</summary>
    void Clear();

    /// <summary>Removes all items belonging to a specific owner.</summary>
    int RemoveByOwner(string ownerKey);

    /// <summary>Removes the item at the specified zero-based position. Returns false if position is out of range.</summary>
    bool RemoveAt(int position);

    /// <summary>Returns a snapshot of all items in fair-schedule order.</summary>
    IReadOnlyList<(T Item, int Rank, string OwnerKey)> GetSnapshot();
}
