// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NoMercyBot.Api.Middleware;

namespace NomercyBot.Api.Tests.Middleware;

public class RequestLoggingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AddsXRequestIdHeader()
    {
        RequestDelegate next = _ => Task.CompletedTask;

        var middleware = new RequestLoggingMiddleware(
            next,
            NullLogger<RequestLoggingMiddleware>.Instance
        );
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.Headers.ContainsKey("X-Request-Id").Should().BeTrue();
        context.Response.Headers["X-Request-Id"].ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task InvokeAsync_CallsNextMiddleware()
    {
        var nextCalled = false;
        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new RequestLoggingMiddleware(
            next,
            NullLogger<RequestLoggingMiddleware>.Instance
        );
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_XRequestId_IsShortGuid()
    {
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new RequestLoggingMiddleware(
            next,
            NullLogger<RequestLoggingMiddleware>.Instance
        );
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        var requestId = context.Response.Headers["X-Request-Id"].ToString();
        requestId.Should().HaveLength(8);
    }

    [Fact]
    public async Task InvokeAsync_EachRequest_HasUniqueRequestId()
    {
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new RequestLoggingMiddleware(
            next,
            NullLogger<RequestLoggingMiddleware>.Instance
        );

        var ctx1 = new DefaultHttpContext();
        var ctx2 = new DefaultHttpContext();

        await middleware.InvokeAsync(ctx1);
        await middleware.InvokeAsync(ctx2);

        var id1 = ctx1.Response.Headers["X-Request-Id"].ToString();
        var id2 = ctx2.Response.Headers["X-Request-Id"].ToString();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_PropagatesException()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("test error");
        var middleware = new RequestLoggingMiddleware(
            next,
            NullLogger<RequestLoggingMiddleware>.Instance
        );
        var context = new DefaultHttpContext();

        var act = () => middleware.InvokeAsync(context);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("test error");
    }
}
