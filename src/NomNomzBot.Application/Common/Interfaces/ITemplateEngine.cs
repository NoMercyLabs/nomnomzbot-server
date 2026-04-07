// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Application.Common.Interfaces;

public interface ITemplateEngine
{
    string Render(string template, IReadOnlyDictionary<string, string> variables);
}
