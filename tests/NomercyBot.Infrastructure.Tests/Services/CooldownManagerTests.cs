// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Infrastructure.Services.General;

namespace NomercyBot.Infrastructure.Tests.Services;

public class CooldownManagerTests
{
    private static CooldownManager Create() => new();

    // ─── IsOnCooldown ─────────────────────────────────────────────────────────

    [Fact]
    public void IsOnCooldown_NoCooldownSet_ReturnsFalse()
    {
        var mgr = Create();
        mgr.IsOnCooldown("chan", "!so").Should().BeFalse();
    }

    [Fact]
    public void IsOnCooldown_AfterSet_ReturnsTrue()
    {
        var mgr = Create();
        mgr.SetCooldown("chan", "!so", TimeSpan.FromSeconds(30));

        mgr.IsOnCooldown("chan", "!so").Should().BeTrue();
    }

    [Fact]
    public void IsOnCooldown_AfterExpiry_ReturnsFalse()
    {
        var mgr = Create();
        mgr.SetCooldown("chan", "!cmd", TimeSpan.FromMilliseconds(1));

        Thread.Sleep(10);

        mgr.IsOnCooldown("chan", "!cmd").Should().BeFalse();
    }

    [Fact]
    public void IsOnCooldown_GlobalCooldown_DoesNotAffectPerUserKey()
    {
        var mgr = Create();
        mgr.SetCooldown("chan", "!so", TimeSpan.FromSeconds(60)); // global

        // Per-user key should not be on cooldown
        mgr.IsOnCooldown("chan", "!so", "user1").Should().BeFalse();
    }

    [Fact]
    public void IsOnCooldown_PerUserCooldown_DoesNotAffectOtherUser()
    {
        var mgr = Create();
        mgr.SetCooldown("chan", "!so", TimeSpan.FromSeconds(60), "user1");

        mgr.IsOnCooldown("chan", "!so", "user2").Should().BeFalse();
    }

    [Fact]
    public void IsOnCooldown_DifferentChannels_Independent()
    {
        var mgr = Create();
        mgr.SetCooldown("chan1", "!cmd", TimeSpan.FromSeconds(60));

        mgr.IsOnCooldown("chan2", "!cmd").Should().BeFalse();
    }

    // ─── GetRemainingCooldown ────────────────────────────────────────────────

    [Fact]
    public void GetRemainingCooldown_NoCooldown_ReturnsNull()
    {
        var mgr = Create();
        mgr.GetRemainingCooldown("chan", "!cmd").Should().BeNull();
    }

    [Fact]
    public void GetRemainingCooldown_ActiveCooldown_ReturnsPositive()
    {
        var mgr = Create();
        mgr.SetCooldown("chan", "!cmd", TimeSpan.FromSeconds(60));

        var remaining = mgr.GetRemainingCooldown("chan", "!cmd");
        remaining.Should().NotBeNull();
        remaining!.Value.Should().BePositive();
        remaining.Value.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void GetRemainingCooldown_AfterExpiry_ReturnsNull()
    {
        var mgr = Create();
        mgr.SetCooldown("chan", "!cmd", TimeSpan.FromMilliseconds(1));

        Thread.Sleep(10);

        mgr.GetRemainingCooldown("chan", "!cmd").Should().BeNull();
    }

    // ─── SetCooldown ──────────────────────────────────────────────────────────

    [Fact]
    public void SetCooldown_Overwrite_UpdatesExpiry()
    {
        var mgr = Create();
        mgr.SetCooldown("chan", "!cmd", TimeSpan.FromSeconds(10));
        mgr.SetCooldown("chan", "!cmd", TimeSpan.FromSeconds(60)); // overwrite

        var remaining = mgr.GetRemainingCooldown("chan", "!cmd");
        remaining.Should().NotBeNull();
        remaining!.Value.Should().BeGreaterThan(TimeSpan.FromSeconds(30));
    }

    // ─── ClearCooldown ────────────────────────────────────────────────────────

    [Fact]
    public void ClearCooldown_ExistingCooldown_RemovesIt()
    {
        var mgr = Create();
        mgr.SetCooldown("chan", "!cmd", TimeSpan.FromSeconds(60));
        mgr.ClearCooldown("chan", "!cmd");

        mgr.IsOnCooldown("chan", "!cmd").Should().BeFalse();
    }

    [Fact]
    public void ClearCooldown_NonExistentKey_DoesNotThrow()
    {
        var mgr = Create();
        var act = () => mgr.ClearCooldown("chan", "!nonexistent");

        act.Should().NotThrow();
    }

    [Fact]
    public void ClearCooldown_PerUser_OnlyClearsUserKey()
    {
        var mgr = Create();
        mgr.SetCooldown("chan", "!so", TimeSpan.FromSeconds(60));          // global
        mgr.SetCooldown("chan", "!so", TimeSpan.FromSeconds(60), "user1"); // per-user

        mgr.ClearCooldown("chan", "!so", "user1");

        mgr.IsOnCooldown("chan", "!so", "user1").Should().BeFalse();
        mgr.IsOnCooldown("chan", "!so").Should().BeTrue(); // global untouched
    }

    // ─── ClearAllCooldowns ────────────────────────────────────────────────────

    [Fact]
    public void ClearAllCooldowns_RemovesAllForChannel()
    {
        var mgr = Create();
        mgr.SetCooldown("chan", "!cmd1", TimeSpan.FromSeconds(60));
        mgr.SetCooldown("chan", "!cmd2", TimeSpan.FromSeconds(60));
        mgr.SetCooldown("chan", "!cmd3", TimeSpan.FromSeconds(60), "user1");
        mgr.SetCooldown("other-chan", "!cmd1", TimeSpan.FromSeconds(60));

        mgr.ClearAllCooldowns("chan");

        mgr.IsOnCooldown("chan", "!cmd1").Should().BeFalse();
        mgr.IsOnCooldown("chan", "!cmd2").Should().BeFalse();
        mgr.IsOnCooldown("chan", "!cmd3", "user1").Should().BeFalse();
        mgr.IsOnCooldown("other-chan", "!cmd1").Should().BeTrue(); // untouched
    }

    [Fact]
    public void ClearAllCooldowns_EmptyChannel_DoesNotThrow()
    {
        var mgr = Create();
        var act = () => mgr.ClearAllCooldowns("nonexistent-channel");

        act.Should().NotThrow();
    }

    // ─── Concurrency ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CooldownManager_ConcurrentAccess_DoesNotThrow()
    {
        var mgr = Create();
        var tasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            mgr.SetCooldown("chan", $"!cmd{i % 5}", TimeSpan.FromSeconds(10), $"user{i}");
            mgr.IsOnCooldown("chan", $"!cmd{i % 5}", $"user{i}");
            mgr.GetRemainingCooldown("chan", $"!cmd{i % 5}", $"user{i}");
        }));

        var act = () => Task.WhenAll(tasks);
        await act.Should().NotThrowAsync();
    }
}
