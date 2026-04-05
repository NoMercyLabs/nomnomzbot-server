// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Infrastructure.Services.Trust;

namespace NomercyBot.Infrastructure.Tests.Services.Trust;

public class TrustScoreCalculatorTests
{
    // ─── Score range ─────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_ZeroInputs_ScoreIsZero()
    {
        var ctx = new TrustContext();
        TrustScoreCalculator.Calculate(ctx).Should().Be(0.0);
    }

    [Fact]
    public void Calculate_ScoreAlwaysInRange_Positive()
    {
        var ctx = new TrustContext
        {
            SuccessfulRequestCount = 1000,
            AccountAgeMonths = 120,
            ContentAgeMonths = 120,
            ContentViewCount = 100_000_000,
            IsFollowing = true,
            FollowAgeDays = 365,
            IsModerator = true,
            IsVip = true,
            IsSubscriber = true,
        };
        TrustScoreCalculator.Calculate(ctx).Should().BeInRange(0, 100);
    }

    [Fact]
    public void Calculate_ScoreAlwaysInRange_WithViolations()
    {
        var ctx = new TrustContext
        {
            BanCount = 100,
            TimeoutCount = 100,
            SkippedByModCount = 100,
        };
        TrustScoreCalculator.Calculate(ctx).Should().BeInRange(0, 100);
    }

    // ─── Follow penalty ──────────────────────────────────────────────────────

    [Fact]
    public void Calculate_NotFollowing_AppliesFollowPenalty()
    {
        var withFollower = new TrustContext
        {
            SuccessfulRequestCount = 5,
            AccountAgeMonths = 12,
            ContentAgeMonths = 6,
            ContentViewCount = 10_000,
            IsFollowing = true,
            FollowAgeDays = 30,
        };

        var withoutFollower = new TrustContext
        {
            SuccessfulRequestCount = 5,
            AccountAgeMonths = 12,
            ContentAgeMonths = 6,
            ContentViewCount = 10_000,
            IsFollowing = false,
            FollowAgeDays = 30,
        };

        TrustScoreCalculator
            .Calculate(withoutFollower)
            .Should()
            .BeLessThan(
                TrustScoreCalculator.Calculate(withFollower),
                "not following reduces the score by 25%"
            );
    }

    [Fact]
    public void Calculate_FollowingLessThan24Hours_AppliesFollowPenalty()
    {
        var newFollower = new TrustContext
        {
            SuccessfulRequestCount = 5,
            AccountAgeMonths = 12,
            ContentAgeMonths = 6,
            ContentViewCount = 10_000,
            IsFollowing = true,
            FollowAgeDays = 0.5,
        };

        var establishedFollower = new TrustContext
        {
            SuccessfulRequestCount = 5,
            AccountAgeMonths = 12,
            ContentAgeMonths = 6,
            ContentViewCount = 10_000,
            IsFollowing = true,
            FollowAgeDays = 30,
        };

        TrustScoreCalculator
            .Calculate(newFollower)
            .Should()
            .BeLessThan(
                TrustScoreCalculator.Calculate(establishedFollower),
                "following < 24h triggers follow penalty"
            );
    }

    // ─── Reputation boost ────────────────────────────────────────────────────

    [Fact]
    public void Calculate_Moderator_BoostsScore()
    {
        var baseCtx = new TrustContext
        {
            SuccessfulRequestCount = 2,
            AccountAgeMonths = 6,
            ContentAgeMonths = 3,
            ContentViewCount = 5_000,
            IsFollowing = true,
            FollowAgeDays = 30,
        };

        var modCtx = new TrustContext
        {
            SuccessfulRequestCount = 2,
            AccountAgeMonths = 6,
            ContentAgeMonths = 3,
            ContentViewCount = 5_000,
            IsFollowing = true,
            FollowAgeDays = 30,
            IsModerator = true,
        };

        TrustScoreCalculator
            .Calculate(modCtx)
            .Should()
            .BeGreaterThan(
                TrustScoreCalculator.Calculate(baseCtx),
                "moderators receive a reputation boost"
            );
    }

    [Fact]
    public void Calculate_TenOrMoreRequests_BoostsScore()
    {
        var regular = new TrustContext
        {
            SuccessfulRequestCount = 9,
            AccountAgeMonths = 6,
            ContentAgeMonths = 3,
            ContentViewCount = 5_000,
            IsFollowing = true,
            FollowAgeDays = 30,
        };

        var veteran = new TrustContext
        {
            SuccessfulRequestCount = 10,
            AccountAgeMonths = 6,
            ContentAgeMonths = 3,
            ContentViewCount = 5_000,
            IsFollowing = true,
            FollowAgeDays = 30,
        };

        TrustScoreCalculator
            .Calculate(veteran)
            .Should()
            .BeGreaterThan(
                TrustScoreCalculator.Calculate(regular),
                "10+ requests triggers reputation boost"
            );
    }

    // ─── Violation penalties ─────────────────────────────────────────────────

    [Fact]
    public void Calculate_WithTimeouts_ReducesScore()
    {
        var good = new TrustContext
        {
            SuccessfulRequestCount = 5,
            AccountAgeMonths = 12,
            ContentAgeMonths = 6,
            ContentViewCount = 50_000,
            IsFollowing = true,
            FollowAgeDays = 90,
            IsSubscriber = true,
        };

        var withViolations = new TrustContext
        {
            SuccessfulRequestCount = 5,
            AccountAgeMonths = 12,
            ContentAgeMonths = 6,
            ContentViewCount = 50_000,
            IsFollowing = true,
            FollowAgeDays = 90,
            IsSubscriber = true,
            TimeoutCount = 3,
        };

        TrustScoreCalculator
            .Calculate(withViolations)
            .Should()
            .BeLessThan(TrustScoreCalculator.Calculate(good), "timeouts reduce the trust score");
    }

    // ─── YouTube penalties ───────────────────────────────────────────────────

    [Fact]
    public void Calculate_YoutubeNewChannel_AppliesPenalty()
    {
        var legitimateYt = new TrustContext
        {
            SuccessfulRequestCount = 5,
            AccountAgeMonths = 12,
            ContentAgeMonths = 6,
            ContentViewCount = 50_000,
            IsFollowing = true,
            FollowAgeDays = 30,
            IsYouTubeContent = true,
            ContentChannelVideoCount = 100,
            ContentChannelTotalViews = 500_000,
            ContentChannelSubscribers = 1_000,
            ContentChannelAgeMonths = 24,
        };

        var spamYt = new TrustContext
        {
            SuccessfulRequestCount = 5,
            AccountAgeMonths = 12,
            ContentAgeMonths = 6,
            ContentViewCount = 50_000,
            IsFollowing = true,
            FollowAgeDays = 30,
            IsYouTubeContent = true,
            ContentChannelVideoCount = 2,
            ContentChannelTotalViews = 100,
            ContentChannelSubscribers = 1,
            ContentChannelAgeMonths = 0.5,
        };

        TrustScoreCalculator
            .Calculate(spamYt)
            .Should()
            .BeLessThan(
                TrustScoreCalculator.Calculate(legitimateYt),
                "new/spam YouTube channels receive lower trust"
            );
    }

    // ─── Tier mapping ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0, TrustTier.Untrusted)]
    [InlineData(25.0, TrustTier.Untrusted)]
    [InlineData(25.1, TrustTier.Low)]
    [InlineData(50.0, TrustTier.Low)]
    [InlineData(50.1, TrustTier.Standard)]
    [InlineData(75.0, TrustTier.Standard)]
    [InlineData(75.1, TrustTier.Trusted)]
    [InlineData(100.0, TrustTier.Trusted)]
    public void GetTier_MapsScoreToCorrectTier(double score, TrustTier expectedTier)
    {
        TrustScoreCalculator.GetTier(score).Should().Be(expectedTier);
    }
}
