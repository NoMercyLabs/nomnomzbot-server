// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Infrastructure.Configuration;

public class TwitchOptions
{
    public const string SectionName = "Twitch";

    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
    public string BotUsername { get; set; } = null!;
    public string RedirectUri { get; set; } = null!;
    public string BotRedirectUri { get; set; } = null!;
    public string ChannelBotRedirectUri { get; set; } = null!;
}
