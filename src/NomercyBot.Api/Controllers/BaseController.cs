// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Api.Models;

namespace NoMercyBot.Api.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected IActionResult UnauthenticatedResponse(string? message = null) =>
        Unauthorized(new StatusResponseDto<object> { Status = "error", Message = message ?? "Unauthorized" });

    protected IActionResult UnauthorizedResponse(string? message = null) =>
        StatusCode(403, new StatusResponseDto<object> { Status = "error", Message = message ?? "Forbidden" });

    protected IActionResult BadRequestResponse(string? message = null) =>
        BadRequest(new StatusResponseDto<object> { Status = "error", Message = message ?? "Bad request" });

    protected IActionResult NotFoundResponse(string? message = null) =>
        NotFound(new StatusResponseDto<object> { Status = "error", Message = message ?? "Not found" });

    protected IActionResult ConflictResponse(string? message = null) =>
        Conflict(new StatusResponseDto<object> { Status = "error", Message = message ?? "Conflict" });

    protected IActionResult TooManyRequestsResponse(string? message = null) =>
        StatusCode(429, new StatusResponseDto<object> { Status = "error", Message = message ?? "Too many requests" });

    protected IActionResult InternalServerErrorResponse(string? message = null) =>
        StatusCode(500, new StatusResponseDto<object> { Status = "error", Message = message ?? "Internal server error" });

    protected IActionResult ServiceUnavailableResponse(string? message = null) =>
        StatusCode(503, new StatusResponseDto<object> { Status = "error", Message = message ?? "Service unavailable" });

    protected IActionResult GetPaginatedResponse<T>(IEnumerable<T> data, PageRequestDto request)
    {
        var items = data.ToList();
        var hasMore = items.Count >= request.Take;
        items = items.Take(request.Take).ToList();

        return Ok(new PaginatedResponse<T>
        {
            Data = items,
            NextPage = hasMore ? request.Page + 1 : null,
            HasMore = hasMore
        });
    }

    protected IActionResult ResultResponse<T>(NoMercyBot.Application.Common.Models.Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(new StatusResponseDto<T> { Data = result.Value });

        return result.ErrorCode switch
        {
            "AUTH_REQUIRED" or "TOKEN_EXPIRED" => UnauthenticatedResponse(result.ErrorMessage),
            "FORBIDDEN" or "FEATURE_DISABLED" or "SCOPE_MISSING" or "BILLING_LIMIT" => UnauthorizedResponse(result.ErrorMessage),
            "NOT_FOUND" or "CHANNEL_NOT_FOUND" or "CHANNEL_NOT_ONBOARDED" => NotFoundResponse(result.ErrorMessage),
            "VALIDATION_FAILED" => BadRequestResponse(result.ErrorMessage),
            "ALREADY_EXISTS" => ConflictResponse(result.ErrorMessage),
            "RATE_LIMITED" => TooManyRequestsResponse(result.ErrorMessage),
            "SERVICE_UNAVAILABLE" => ServiceUnavailableResponse(result.ErrorMessage),
            _ => InternalServerErrorResponse(result.ErrorMessage)
        };
    }

    protected IActionResult ResultResponse(NoMercyBot.Application.Common.Models.Result result)
    {
        if (result.IsSuccess)
            return Ok(new StatusResponseDto<object> { Status = "ok" });

        return result.ErrorCode switch
        {
            "AUTH_REQUIRED" or "TOKEN_EXPIRED" => UnauthenticatedResponse(result.ErrorMessage),
            "FORBIDDEN" or "FEATURE_DISABLED" or "SCOPE_MISSING" or "BILLING_LIMIT" => UnauthorizedResponse(result.ErrorMessage),
            "NOT_FOUND" or "CHANNEL_NOT_FOUND" or "CHANNEL_NOT_ONBOARDED" => NotFoundResponse(result.ErrorMessage),
            "VALIDATION_FAILED" => BadRequestResponse(result.ErrorMessage),
            "ALREADY_EXISTS" => ConflictResponse(result.ErrorMessage),
            "RATE_LIMITED" => TooManyRequestsResponse(result.ErrorMessage),
            "SERVICE_UNAVAILABLE" => ServiceUnavailableResponse(result.ErrorMessage),
            _ => InternalServerErrorResponse(result.ErrorMessage)
        };
    }

    protected IActionResult GetPaginatedResponse<T>(
        NoMercyBot.Application.Common.Models.PagedList<T> pagedList,
        PageRequestDto request)
    {
        return Ok(new PaginatedResponse<T>
        {
            Data = pagedList.Items,
            NextPage = pagedList.HasNextPage ? pagedList.Page + 1 : null,
            HasMore = pagedList.HasNextPage,
        });
    }
}
