// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Infrastructure.Collections;

namespace NomercyBot.Infrastructure.Tests.Collections;

public class FairQueueTests
{
    // ─── Basic enqueue / dequeue ─────────────────────────────────────────────

    [Fact]
    public void Dequeue_SingleItem_ReturnsItem()
    {
        var q = new FairQueue<string>();
        q.Enqueue("alice", "track-a");

        q.Dequeue().Should().Be("track-a");
        q.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Dequeue_Empty_ReturnsDefault()
    {
        var q = new FairQueue<string>();
        q.Dequeue().Should().BeNull();
    }

    [Fact]
    public void Count_ReflectsQueuedItems()
    {
        var q = new FairQueue<string>();
        q.Enqueue("alice", "a");
        q.Enqueue("alice", "b");
        q.Enqueue("bob", "c");

        q.Count.Should().Be(3);
    }

    // ─── Fairness algorithm ──────────────────────────────────────────────────

    [Fact]
    public void Enqueue_TwoOwners_InterleavesBeforeRepeat()
    {
        var q = new FairQueue<string>();
        // alice adds 2, bob adds 1
        q.Enqueue("alice", "a1");
        q.Enqueue("alice", "a2");
        q.Enqueue("bob", "b1");

        // Expected order: a1, b1, a2  (rank 1 items first, then rank 2)
        q.Dequeue().Should().Be("a1");
        q.Dequeue().Should().Be("b1");
        q.Dequeue().Should().Be("a2");
    }

    [Fact]
    public void Enqueue_ThreeOwners_RoundRobinOrdering()
    {
        var q = new FairQueue<string>();
        // Each user enqueues 2 songs
        q.Enqueue("alice", "a1");
        q.Enqueue("alice", "a2");
        q.Enqueue("bob", "b1");
        q.Enqueue("bob", "b2");
        q.Enqueue("carol", "c1");
        q.Enqueue("carol", "c2");

        // Round 1: all rank-1 items (a1, b1, c1) come before round 2 (a2, b2, c2)
        var dequeued = Enumerable.Range(0, 6).Select(_ => q.Dequeue()).ToList();

        dequeued.Should().BeEquivalentTo(["a1", "a2", "b1", "b2", "c1", "c2"]);

        // The first three should be the round-1 items, in insertion order
        dequeued[..3].Should().BeEquivalentTo(["a1", "b1", "c1"],
            "rank-1 items from all owners come before rank-2 items");
        dequeued[3..].Should().BeEquivalentTo(["a2", "b2", "c2"],
            "rank-2 items follow after all rank-1 items");
    }

    [Fact]
    public void Enqueue_SingleOwner_FifoOrder()
    {
        var q = new FairQueue<string>();
        q.Enqueue("alice", "1");
        q.Enqueue("alice", "2");
        q.Enqueue("alice", "3");

        q.Dequeue().Should().Be("1");
        q.Dequeue().Should().Be("2");
        q.Dequeue().Should().Be("3");
    }

    // ─── Dequeue recalculation ───────────────────────────────────────────────

    [Fact]
    public void Dequeue_AfterDequeuingFirstItem_RanksRecalculated()
    {
        var q = new FairQueue<string>();
        q.Enqueue("alice", "a1");
        q.Enqueue("alice", "a2");
        q.Enqueue("alice", "a3");

        q.Dequeue(); // removes a1, a2 becomes rank 1, a3 becomes rank 2

        // Now add a new item — should go after a2 (same rank 1 → end of rank-1 group)
        q.Enqueue("alice", "a4");

        // remaining: a2 (rank 1), a3 (rank 2) → a4 was added as rank 2 (alice already has a3)
        // so order: a2, a3, a4
        q.Dequeue().Should().Be("a2");
    }

    // ─── Peek ────────────────────────────────────────────────────────────────

    [Fact]
    public void Peek_DoesNotRemoveItem()
    {
        var q = new FairQueue<string>();
        q.Enqueue("alice", "a1");

        q.Peek().Should().Be("a1");
        q.Count.Should().Be(1);
    }

    // ─── Clear ───────────────────────────────────────────────────────────────

    [Fact]
    public void Clear_EmptiesQueue()
    {
        var q = new FairQueue<string>();
        q.Enqueue("alice", "a1");
        q.Enqueue("bob", "b1");

        q.Clear();

        q.IsEmpty.Should().BeTrue();
        q.Count.Should().Be(0);
    }

    // ─── RemoveByOwner ───────────────────────────────────────────────────────

    [Fact]
    public void RemoveByOwner_RemovesAllItemsForOwner()
    {
        var q = new FairQueue<string>();
        q.Enqueue("alice", "a1");
        q.Enqueue("alice", "a2");
        q.Enqueue("bob", "b1");

        q.RemoveByOwner("alice").Should().Be(2);

        q.Count.Should().Be(1);
        q.Dequeue().Should().Be("b1");
    }

    [Fact]
    public void RemoveByOwner_OwnerNotPresent_ReturnsZero()
    {
        var q = new FairQueue<string>();
        q.Enqueue("alice", "a1");

        q.RemoveByOwner("nobody").Should().Be(0);
        q.Count.Should().Be(1);
    }

    // ─── GetSnapshot ─────────────────────────────────────────────────────────

    [Fact]
    public void GetSnapshot_ReturnsCurrentState()
    {
        var q = new FairQueue<string>();
        q.Enqueue("alice", "a1");
        q.Enqueue("bob", "b1");

        var snapshot = q.GetSnapshot();

        snapshot.Should().HaveCount(2);
        snapshot.Should().Contain(s => s.Item == "a1" && s.OwnerKey == "alice");
        snapshot.Should().Contain(s => s.Item == "b1" && s.OwnerKey == "bob");
    }

    // ─── Thread safety ───────────────────────────────────────────────────────

    [Fact]
    public async Task Enqueue_ConcurrentAccess_DoesNotCorruptState()
    {
        var q = new FairQueue<int>();
        const int iterations = 100;

        var tasks = Enumerable.Range(0, 10).Select(ownerId =>
            Task.Run(() =>
            {
                for (int i = 0; i < iterations; i++)
                    q.Enqueue($"owner{ownerId}", ownerId * iterations + i);
            }));

        await Task.WhenAll(tasks);

        q.Count.Should().Be(10 * iterations);
    }
}
