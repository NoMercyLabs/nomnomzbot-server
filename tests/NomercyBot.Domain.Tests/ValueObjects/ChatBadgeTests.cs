// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Domain.ValueObjects;

namespace NomercyBot.Domain.Tests.ValueObjects;

public class ChatBadgeTests
{
    [Fact]
    public void ChatBadge_CreatesWithSetIdAndId()
    {
        var badge = new ChatBadge("subscriber", "3");

        badge.SetId.Should().Be("subscriber");
        badge.Id.Should().Be("3");
        badge.Info.Should().BeNull();
    }

    [Fact]
    public void ChatBadge_CreatesWithInfo()
    {
        var badge = new ChatBadge("subscriber", "3", "3 months");

        badge.Info.Should().Be("3 months");
    }

    [Fact]
    public void ChatBadge_RecordEquality_SameValues_AreEqual()
    {
        var a = new ChatBadge("moderator", "1");
        var b = new ChatBadge("moderator", "1");

        a.Should().Be(b);
    }

    [Fact]
    public void ChatBadge_RecordEquality_DifferentSetId_AreNotEqual()
    {
        var a = new ChatBadge("moderator", "1");
        var b = new ChatBadge("subscriber", "1");

        a.Should().NotBe(b);
    }

    [Fact]
    public void ChatBadge_RecordEquality_DifferentId_AreNotEqual()
    {
        var a = new ChatBadge("subscriber", "1");
        var b = new ChatBadge("subscriber", "3");

        a.Should().NotBe(b);
    }

    [Fact]
    public void ChatBadge_RecordEquality_InfoDiffers_AreNotEqual()
    {
        var a = new ChatBadge("subscriber", "3", "3 months");
        var b = new ChatBadge("subscriber", "3", "6 months");

        a.Should().NotBe(b);
    }

    [Fact]
    public void ChatBadge_ToString_ContainsFields()
    {
        var badge = new ChatBadge("vip", "1");
        badge.ToString().Should().Contain("vip").And.Contain("1");
    }
}

public class ChatMessageFragmentTests
{
    [Fact]
    public void ChatMessageFragment_DefaultType_IsText()
    {
        var fragment = new ChatMessageFragment();
        fragment.Type.Should().Be("text");
    }

    [Fact]
    public void ChatMessageFragment_DefaultText_IsEmpty()
    {
        var fragment = new ChatMessageFragment();
        fragment.Text.Should().BeEmpty();
    }

    [Fact]
    public void ChatMessageFragment_TextFragment_SetProperties()
    {
        var fragment = new ChatMessageFragment { Type = "text", Text = "Hello world" };

        fragment.Type.Should().Be("text");
        fragment.Text.Should().Be("Hello world");
    }

    [Fact]
    public void ChatMessageFragment_EmoteFragment_SetsEmoteFields()
    {
        var fragment = new ChatMessageFragment
        {
            Type = "emote",
            Text = "Kappa",
            EmoteId = "25",
            EmoteSetId = "0",
            EmoteOwnerId = "broadcaster123",
            EmoteFormats = ["static", "animated"],
        };

        fragment.Type.Should().Be("emote");
        fragment.EmoteId.Should().Be("25");
        fragment.EmoteSetId.Should().Be("0");
        fragment.EmoteOwnerId.Should().Be("broadcaster123");
        fragment.EmoteFormats.Should().BeEquivalentTo(["static", "animated"]);
    }

    [Fact]
    public void ChatMessageFragment_CheermoteFragment_SetsCheermoteFields()
    {
        var fragment = new ChatMessageFragment
        {
            Type = "cheermote",
            Text = "Cheer100",
            CheermotePrefix = "Cheer",
            CheermoteBits = 100,
            CheermoteTier = 1,
        };

        fragment.Type.Should().Be("cheermote");
        fragment.CheermotePrefix.Should().Be("Cheer");
        fragment.CheermoteBits.Should().Be(100);
        fragment.CheermoteTier.Should().Be(1);
    }

    [Fact]
    public void ChatMessageFragment_MentionFragment_SetsMentionFields()
    {
        var fragment = new ChatMessageFragment
        {
            Type = "mention",
            Text = "@someuser",
            MentionUserId = "uid999",
            MentionUserLogin = "someuser",
            MentionUserName = "SomeUser",
        };

        fragment.Type.Should().Be("mention");
        fragment.MentionUserId.Should().Be("uid999");
        fragment.MentionUserLogin.Should().Be("someuser");
        fragment.MentionUserName.Should().Be("SomeUser");
    }

    [Fact]
    public void ChatMessageFragment_NonEmoteFragment_EmoteFieldsAreNull()
    {
        var fragment = new ChatMessageFragment { Type = "text", Text = "hi" };

        fragment.EmoteId.Should().BeNull();
        fragment.EmoteSetId.Should().BeNull();
        fragment.EmoteFormats.Should().BeEmpty();
    }

    [Fact]
    public void ChatMessageFragment_NonCheermoteFragment_CheermoteFieldsAreNull()
    {
        var fragment = new ChatMessageFragment { Type = "text", Text = "hi" };

        fragment.CheermotePrefix.Should().BeNull();
        fragment.CheermoteBits.Should().BeNull();
        fragment.CheermoteTier.Should().BeNull();
    }
}
