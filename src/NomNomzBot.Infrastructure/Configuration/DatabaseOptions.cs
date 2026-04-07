// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Infrastructure.Configuration;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; set; } = null!;
    public int MaxPoolSize { get; set; } = 20;
    public int MinPoolSize { get; set; } = 5;
}
