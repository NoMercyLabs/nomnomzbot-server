// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Api.Middleware;
using NoMercyBot.Application.Common.Interfaces;
using NSubstitute;

namespace NomercyBot.Api.Tests.Middleware;

public class TenantResolutionMiddlewareTests
{
    private static TenantResolutionMiddleware CreateMiddleware(RequestDelegate? next = null)
    {
        next ??= _ => Task.CompletedTask;
        return new(next);
    }

    [Fact]
    public async Task InvokeAsync_RouteValue_SetsTenant()
    {
        TenantResolutionMiddleware middleware = CreateMiddleware();
        ICurrentTenantService? tenantService = Substitute.For<ICurrentTenantService>();
        DefaultHttpContext context = new();
        context.Request.RouteValues["channelId"] = "chan-route-123";

        await middleware.InvokeAsync(context, tenantService);

        tenantService.Received(1).SetTenant("chan-route-123");
    }

    [Fact]
    public async Task InvokeAsync_XChannelIdHeader_SetsTenant()
    {
        TenantResolutionMiddleware middleware = CreateMiddleware();
        ICurrentTenantService? tenantService = Substitute.For<ICurrentTenantService>();
        DefaultHttpContext context = new();
        context.Request.Headers["X-Channel-Id"] = "chan-header-456";

        await middleware.InvokeAsync(context, tenantService);

        tenantService.Received(1).SetTenant("chan-header-456");
    }

    [Fact]
    public async Task InvokeAsync_QueryString_SetsTenant()
    {
        TenantResolutionMiddleware middleware = CreateMiddleware();
        ICurrentTenantService? tenantService = Substitute.For<ICurrentTenantService>();
        DefaultHttpContext context = new();
        context.Request.QueryString = new("?channelId=chan-query-789");

        await middleware.InvokeAsync(context, tenantService);

        tenantService.Received(1).SetTenant("chan-query-789");
    }

    [Fact]
    public async Task InvokeAsync_NoSource_DoesNotSetTenant()
    {
        TenantResolutionMiddleware middleware = CreateMiddleware();
        ICurrentTenantService? tenantService = Substitute.For<ICurrentTenantService>();
        DefaultHttpContext context = new();

        await middleware.InvokeAsync(context, tenantService);

        tenantService.DidNotReceive().SetTenant(Arg.Any<string>());
    }

    [Fact]
    public async Task InvokeAsync_RouteValueTakesPrecedenceOverHeader()
    {
        TenantResolutionMiddleware middleware = CreateMiddleware();
        ICurrentTenantService? tenantService = Substitute.For<ICurrentTenantService>();
        DefaultHttpContext context = new();
        context.Request.RouteValues["channelId"] = "from-route";
        context.Request.Headers["X-Channel-Id"] = "from-header";

        await middleware.InvokeAsync(context, tenantService);

        tenantService.Received(1).SetTenant("from-route");
        tenantService.DidNotReceive().SetTenant("from-header");
    }

    [Fact]
    public async Task InvokeAsync_HeaderTakesPrecedenceOverQuery()
    {
        TenantResolutionMiddleware middleware = CreateMiddleware();
        ICurrentTenantService? tenantService = Substitute.For<ICurrentTenantService>();
        DefaultHttpContext context = new();
        context.Request.Headers["X-Channel-Id"] = "from-header";
        context.Request.QueryString = new("?channelId=from-query");

        await middleware.InvokeAsync(context, tenantService);

        tenantService.Received(1).SetTenant("from-header");
        tenantService.DidNotReceive().SetTenant("from-query");
    }

    [Fact]
    public async Task InvokeAsync_AlwaysCallsNext()
    {
        bool nextCalled = false;
        TenantResolutionMiddleware middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        ICurrentTenantService? tenantService = Substitute.For<ICurrentTenantService>();
        DefaultHttpContext context = new();

        await middleware.InvokeAsync(context, tenantService);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_EmptyRouteValue_DoesNotSetTenant()
    {
        TenantResolutionMiddleware middleware = CreateMiddleware();
        ICurrentTenantService? tenantService = Substitute.For<ICurrentTenantService>();
        DefaultHttpContext context = new();
        context.Request.RouteValues["channelId"] = ""; // empty string

        await middleware.InvokeAsync(context, tenantService);

        tenantService.DidNotReceive().SetTenant(Arg.Any<string>());
    }
}
