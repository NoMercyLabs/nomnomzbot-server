// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.IO;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NoMercyBot.Api.Middleware;
using NSubstitute;

// IHostEnvironment is in Microsoft.Extensions.Hosting.Abstractions
using IHostEnvironment = Microsoft.Extensions.Hosting.IHostEnvironment;

namespace NomercyBot.Api.Tests.Middleware;

public class GlobalExceptionMiddlewareTests
{
    private static GlobalExceptionMiddleware CreateMiddleware(
        RequestDelegate next,
        bool isDevelopment = false)
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(isDevelopment ? "Development" : "Production");
        return new GlobalExceptionMiddleware(next, NullLogger<GlobalExceptionMiddleware>.Instance, env);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Fact]
    public async Task InvokeAsync_NoException_PassesThrough()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = CreateContext();

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200); // unchanged
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_Returns500()
    {
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("boom"));
        var context = CreateContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_SetsJsonContentType()
    {
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("boom"));
        var context = CreateContext();

        await middleware.InvokeAsync(context);

        context.Response.ContentType.Should().Contain("application/json");
    }

    [Fact]
    public async Task InvokeAsync_Production_ReturnsGenericMessage()
    {
        var middleware = CreateMiddleware(
            _ => throw new InvalidOperationException("secret internal error"),
            isDevelopment: false);

        var context = CreateContext();
        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        body.Should().NotContain("secret internal error");
        body.Should().Contain("unexpected error");
    }

    [Fact]
    public async Task InvokeAsync_Development_ReturnsActualMessage()
    {
        var middleware = CreateMiddleware(
            _ => throw new InvalidOperationException("detailed dev error"),
            isDevelopment: true);

        var context = CreateContext();
        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        body.Should().Contain("detailed dev error");
    }

    [Fact]
    public async Task InvokeAsync_RequestCancelled_DoesNotReturn500()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var middleware = CreateMiddleware(_ => throw new OperationCanceledException("cancelled"));
        var context = CreateContext();
        context.RequestAborted = cts.Token;

        await middleware.InvokeAsync(context);

        // Should silently swallow OperationCanceledException when request is aborted
        context.Response.StatusCode.Should().Be(200); // not 500
    }

    [Fact]
    public async Task InvokeAsync_ResponseBody_IsValidJson()
    {
        var middleware = CreateMiddleware(_ => throw new Exception("test"), isDevelopment: false);
        var context = CreateContext();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        var act = () => JsonDocument.Parse(body);
        act.Should().NotThrow("response body should be valid JSON");
    }
}
