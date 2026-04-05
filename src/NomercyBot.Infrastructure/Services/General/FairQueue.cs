// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.Services.General;

/// <summary>
/// Fair queue implementation using rank-based scheduling.
///
/// Algorithm:
///   - Each owner has a "usage count" that increments each time one of their items is dequeued.
///   - When enqueuing, an item is inserted after all items whose owner has the same or lower
///     usage count — ensuring users who have requested fewer items are served first.
///   - On dequeue, the front item is removed and the owner's usage count increments.
///
/// Result: users with fewer items in the queue get served before power-users.
/// This is a fair round-robin without per-slot reservation overhead.
/// </summary>
public sealed class FairQueue<T> : IFairQueue<T>
{
    private readonly LinkedList<(string OwnerKey, T Item)> _items = new();
    private readonly Dictionary<string, int> _usageCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public int Count
    {
        get
        {
            lock (_lock)
                return _items.Count;
        }
    }

    public bool IsEmpty
    {
        get
        {
            lock (_lock)
                return _items.Count == 0;
        }
    }

    public void Enqueue(string ownerKey, T item)
    {
        lock (_lock)
        {
            int ownerUsage = _usageCounts.GetValueOrDefault(ownerKey, 0);

            // Find the insertion point: after the last node whose owner usage <= this owner's usage
            // This places the new item after users who have requested the same amount or less,
            // but before users who have requested more.
            LinkedListNode<(string OwnerKey, T Item)>? insertAfter = FindInsertionPoint(ownerUsage);

            if (insertAfter is null)
                _items.AddFirst((ownerKey, item));
            else
                _items.AddAfter(insertAfter, (ownerKey, item));
        }
    }

    public T? Dequeue()
    {
        lock (_lock)
        {
            if (_items.Count == 0)
                return default;

            LinkedListNode<(string OwnerKey, T Item)>? node = _items.First!;
            (string ownerKey, T item) = node.Value;
            _items.RemoveFirst();

            _usageCounts[ownerKey] = _usageCounts.GetValueOrDefault(ownerKey, 0) + 1;

            return item;
        }
    }

    public T? Peek()
    {
        lock (_lock)
        {
            if (_items.Count == 0)
                return default;
            return _items.First!.Value.Item;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _items.Clear();
            _usageCounts.Clear();
        }
    }

    public int RemoveByOwner(string ownerKey)
    {
        lock (_lock)
        {
            int removed = 0;
            LinkedListNode<(string OwnerKey, T Item)>? node = _items.First;
            while (node is not null)
            {
                LinkedListNode<(string OwnerKey, T Item)>? next = node.Next;
                if (
                    string.Equals(node.Value.OwnerKey, ownerKey, StringComparison.OrdinalIgnoreCase)
                )
                {
                    _items.Remove(node);
                    removed++;
                }
                node = next;
            }
            return removed;
        }
    }

    public bool RemoveAt(int position)
    {
        lock (_lock)
        {
            if (position < 0 || position >= _items.Count)
                return false;

            LinkedListNode<(string OwnerKey, T Item)>? node = _items.First;
            for (int i = 0; i < position; i++)
                node = node!.Next;

            _items.Remove(node!);
            return true;
        }
    }

    public IReadOnlyList<(T Item, int Rank, string OwnerKey)> GetSnapshot()
    {
        lock (_lock)
        {
            List<(T Item, int Rank, string OwnerKey)> result = new();
            int rank = 1;
            foreach ((string ownerKey, T item) in _items)
                result.Add((item, rank++, ownerKey));
            return result;
        }
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns the last node whose owner has usage count &lt;= ownerUsage.
    /// Returns null if the new item should be inserted at the front.
    /// </summary>
    private LinkedListNode<(string OwnerKey, T Item)>? FindInsertionPoint(int ownerUsage)
    {
        LinkedListNode<(string OwnerKey, T Item)>? lastEligible = null;
        LinkedListNode<(string OwnerKey, T Item)>? node = _items.First;

        while (node is not null)
        {
            int nodeOwnerUsage = _usageCounts.GetValueOrDefault(node.Value.OwnerKey, 0);
            if (nodeOwnerUsage <= ownerUsage)
                lastEligible = node;
            node = node.Next;
        }

        return lastEligible;
    }
}
