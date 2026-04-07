// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Infrastructure.Configuration;

public class EncryptionOptions
{
    public const string SectionName = "Encryption";

    /// <summary>
    /// Base64-encoded secret key used for AES-256 token encryption.
    /// Must be the same across restarts — store in environment variable ENCRYPTION__KEY.
    /// </summary>
    public string Key { get; set; } = null!;
}
