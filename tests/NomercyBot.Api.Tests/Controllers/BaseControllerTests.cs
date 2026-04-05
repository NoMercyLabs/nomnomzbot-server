// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Api.Controllers;
using NoMercyBot.Application.Common.Models;

namespace NomercyBot.Api.Tests.Controllers;

/// <summary>
/// Concrete subclass that exposes BaseController protected helpers for testing.
/// </summary>
internal sealed class TestController : BaseController
{
    public IActionResult TestUnauthenticated(string? msg = null) => UnauthenticatedResponse(msg);

    public IActionResult TestUnauthorized(string? msg = null) => UnauthorizedResponse(msg);

    public IActionResult TestBadRequest(string? msg = null) => BadRequestResponse(msg);

    public IActionResult TestNotFound(string? msg = null) => NotFoundResponse(msg);

    public IActionResult TestConflict(string? msg = null) => ConflictResponse(msg);

    public IActionResult TestTooManyRequests(string? msg = null) => TooManyRequestsResponse(msg);

    public IActionResult TestInternalServerError(string? msg = null) =>
        InternalServerErrorResponse(msg);

    public IActionResult TestServiceUnavailable(string? msg = null) =>
        ServiceUnavailableResponse(msg);

    public IActionResult TestResultResponse<T>(Result<T> result) => ResultResponse(result);

    public IActionResult TestResultResponse(Result result) => ResultResponse(result);
}

public class BaseControllerTests
{
    private static TestController CreateController()
    {
        var ctrl = new TestController();
        ctrl.ControllerContext = new ControllerContext();
        return ctrl;
    }

    // ─── Status code helpers ──────────────────────────────────────────────────

    [Fact]
    public void UnauthenticatedResponse_Returns401()
    {
        var ctrl = CreateController();
        var result = ctrl.TestUnauthenticated() as ObjectResult;
        result!.StatusCode.Should().Be(401);
    }

    [Fact]
    public void UnauthorizedResponse_Returns403()
    {
        var ctrl = CreateController();
        var result = ctrl.TestUnauthorized() as ObjectResult;
        result!.StatusCode.Should().Be(403);
    }

    [Fact]
    public void BadRequestResponse_Returns400()
    {
        var ctrl = CreateController();
        var result = ctrl.TestBadRequest() as ObjectResult;
        result!.StatusCode.Should().Be(400);
    }

    [Fact]
    public void NotFoundResponse_Returns404()
    {
        var ctrl = CreateController();
        var result = ctrl.TestNotFound() as ObjectResult;
        result!.StatusCode.Should().Be(404);
    }

    [Fact]
    public void ConflictResponse_Returns409()
    {
        var ctrl = CreateController();
        var result = ctrl.TestConflict() as ObjectResult;
        result!.StatusCode.Should().Be(409);
    }

    [Fact]
    public void TooManyRequestsResponse_Returns429()
    {
        var ctrl = CreateController();
        var result = ctrl.TestTooManyRequests() as ObjectResult;
        result!.StatusCode.Should().Be(429);
    }

    [Fact]
    public void InternalServerErrorResponse_Returns500()
    {
        var ctrl = CreateController();
        var result = ctrl.TestInternalServerError() as ObjectResult;
        result!.StatusCode.Should().Be(500);
    }

    [Fact]
    public void ServiceUnavailableResponse_Returns503()
    {
        var ctrl = CreateController();
        var result = ctrl.TestServiceUnavailable() as ObjectResult;
        result!.StatusCode.Should().Be(503);
    }

    // ─── ResultResponse<T> ────────────────────────────────────────────────────

    [Fact]
    public void ResultResponseT_Success_Returns200()
    {
        var ctrl = CreateController();
        var result = ctrl.TestResultResponse(Result.Success("hello")) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Fact]
    public void ResultResponseT_AuthRequired_Returns401()
    {
        var ctrl = CreateController();
        var result =
            ctrl.TestResultResponse(Errors.NotAuthenticated().ToTyped<string>()) as ObjectResult;

        result!.StatusCode.Should().Be(401);
    }

    [Fact]
    public void ResultResponseT_NotFound_Returns404()
    {
        var ctrl = CreateController();
        var result = ctrl.TestResultResponse(Errors.NotFound<string>("User", "u1")) as ObjectResult;

        result!.StatusCode.Should().Be(404);
    }

    [Fact]
    public void ResultResponseT_ValidationFailed_Returns400()
    {
        var ctrl = CreateController();
        var result =
            ctrl.TestResultResponse(Errors.ValidationFailed("bad").ToTyped<string>())
            as ObjectResult;

        result!.StatusCode.Should().Be(400);
    }

    [Fact]
    public void ResultResponseT_AlreadyExists_Returns409()
    {
        var ctrl = CreateController();
        var result =
            ctrl.TestResultResponse(Errors.AlreadyExists("User", "alice").ToTyped<string>())
            as ObjectResult;

        result!.StatusCode.Should().Be(409);
    }

    [Fact]
    public void ResultResponseT_RateLimited_Returns429()
    {
        var ctrl = CreateController();
        var result =
            ctrl.TestResultResponse(
                Errors.RateLimited("login", TimeSpan.FromSeconds(30)).ToTyped<string>()
            ) as ObjectResult;

        result!.StatusCode.Should().Be(429);
    }

    [Fact]
    public void ResultResponseT_ServiceUnavailable_Returns503()
    {
        var ctrl = CreateController();
        var result =
            ctrl.TestResultResponse(Errors.ExternalServiceUnavailable("Spotify").ToTyped<string>())
            as ObjectResult;

        result!.StatusCode.Should().Be(503);
    }

    [Fact]
    public void ResultResponseT_Forbidden_Returns403()
    {
        var ctrl = CreateController();
        var result =
            ctrl.TestResultResponse(Errors.InsufficientPermission("delete").ToTyped<string>())
            as ObjectResult;

        result!.StatusCode.Should().Be(403);
    }

    // ─── ResultResponse (void) ────────────────────────────────────────────────

    [Fact]
    public void ResultResponse_Success_Returns200()
    {
        var ctrl = CreateController();
        var result = ctrl.TestResultResponse(Result.Success()) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Fact]
    public void ResultResponse_Failure_Returns500ForUnknownCode()
    {
        var ctrl = CreateController();
        var failure = Result.Failure("internal", "UNKNOWN_CODE");
        var result = ctrl.TestResultResponse(failure) as ObjectResult;

        result!.StatusCode.Should().Be(500);
    }

    [Fact]
    public void ResultResponse_TokenExpired_Returns401()
    {
        var ctrl = CreateController();
        var result = ctrl.TestResultResponse(Errors.TokenExpired("Twitch")) as ObjectResult;

        result!.StatusCode.Should().Be(401);
    }

    [Fact]
    public void ResultResponse_ChannelNotFound_Returns404()
    {
        var ctrl = CreateController();
        var result = ctrl.TestResultResponse(Errors.ChannelNotFound("ch1")) as ObjectResult;

        result!.StatusCode.Should().Be(404);
    }

    [Fact]
    public void ResultResponse_FeatureDisabled_Returns403()
    {
        var ctrl = CreateController();
        var result =
            ctrl.TestResultResponse(Errors.FeatureNotEnabled("SongRequests")) as ObjectResult;

        result!.StatusCode.Should().Be(403);
    }
}
