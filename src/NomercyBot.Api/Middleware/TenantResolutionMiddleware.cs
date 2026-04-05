// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Application.Common.Interfaces;

namespace NoMercyBot.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenantService tenantService)
    {
        // 1. Route value: /channels/{channelId}/...
        if (
            context.Request.RouteValues.TryGetValue("channelId", out var channelId)
            && channelId is string channelIdStr
            && !string.IsNullOrEmpty(channelIdStr)
        )
        {
            tenantService.SetTenant(channelIdStr);
        }
        // 2. Custom header
        else if (
            context.Request.Headers.TryGetValue("X-Channel-Id", out var headerVal)
            && !string.IsNullOrEmpty(headerVal)
        )
        {
            tenantService.SetTenant(headerVal!);
        }
        // 3. Query string
        else if (
            context.Request.Query.TryGetValue("channelId", out var queryVal)
            && !string.IsNullOrEmpty(queryVal)
        )
        {
            tenantService.SetTenant(queryVal!);
        }

        await _next(context);
    }
}
