// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Application.Common.Models;

namespace NomercyBot.Application.Tests.Models;

public class ResultTests
{
    // ─── Result (non-generic) ────────────────────────────────────────────────

    [Fact]
    public void Result_Success_IsSuccess()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.ErrorMessage.Should().BeNull();
        result.ErrorCode.Should().BeNull();
        result.ErrorDetail.Should().BeNull();
    }

    [Fact]
    public void Result_Failure_IsFailure()
    {
        var result = Result.Failure("something broke");

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be("something broke");
    }

    [Fact]
    public void Result_Failure_WithErrorCode_SetsCode()
    {
        var result = Result.Failure("bad request", "VALIDATION_FAILED");

        result.ErrorCode.Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public void Result_Failure_WithAllFields_SetsAll()
    {
        var result = Result.Failure("error", "ERR_CODE", "extra detail");

        result.ErrorMessage.Should().Be("error");
        result.ErrorCode.Should().Be("ERR_CODE");
        result.ErrorDetail.Should().Be("extra detail");
    }

    // ─── Result<T> ────────────────────────────────────────────────────────────

    [Fact]
    public void ResultT_Success_HasValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ResultT_Success_NullableValue_Succeeds()
    {
        var result = Result.Success<string?>(null);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ResultT_Failure_IsFailure()
    {
        var result = Result.Failure<int>("something wrong");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("something wrong");
    }

    [Fact]
    public void ResultT_Failure_AccessingValue_Throws()
    {
        var result = Result.Failure<int>("broken");

        var act = () =>
        {
            var _ = result.Value;
        };
        act.Should().Throw<InvalidOperationException>().WithMessage("*failed Result*");
    }

    [Fact]
    public void Result_Success_WithValue_CreatesTypedSuccess()
    {
        var voidResult = Result.Success();
        var typed = voidResult.WithValue("hello");

        typed.IsSuccess.Should().BeTrue();
        typed.Value.Should().Be("hello");
    }

    [Fact]
    public void Result_Failure_WithValue_PropagatesError()
    {
        var voidResult = Result.Failure("broke", "CODE", "detail");
        var typed = voidResult.WithValue("ignored");

        typed.IsSuccess.Should().BeFalse();
        typed.ErrorMessage.Should().Be("broke");
        typed.ErrorCode.Should().Be("CODE");
        typed.ErrorDetail.Should().Be("detail");
    }
}

public class ResultExtensionsTests
{
    // ─── Map ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Map_Success_TransformsValue()
    {
        var result = Result.Success(5).Map(x => x * 2);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(10);
    }

    [Fact]
    public void Map_Failure_PropagatesError()
    {
        var result = Result.Failure<int>("oops", "ERR").Map(x => x * 2);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("oops");
        result.ErrorCode.Should().Be("ERR");
    }

    // ─── Bind ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Bind_Success_CallsBinder()
    {
        var result = Result.Success("hello").Bind(s => Result.Success(s.Length));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(5);
    }

    [Fact]
    public void Bind_Success_BinderReturnsFailure_PropagatesFailure()
    {
        var result = Result.Success("hello").Bind<string, int>(_ => Result.Failure<int>("no good"));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("no good");
    }

    [Fact]
    public void Bind_Failure_DoesNotCallBinder()
    {
        var called = false;
        var result = Result
            .Failure<string>("original error")
            .Bind<string, int>(s =>
            {
                called = true;
                return Result.Success(s.Length);
            });

        called.Should().BeFalse();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("original error");
    }

    // ─── BindAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task BindAsync_Success_CallsAsyncBinder()
    {
        var result = await Result
            .Success(10)
            .BindAsync(x => Task.FromResult(Result.Success(x + 5)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(15);
    }

    [Fact]
    public async Task BindAsync_Failure_DoesNotCallBinder()
    {
        var called = false;
        var result = await Result
            .Failure<int>("error")
            .BindAsync(x =>
            {
                called = true;
                return Task.FromResult(Result.Success(x));
            });

        called.Should().BeFalse();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task BindAsync_TaskOverload_Success_CallsBinder()
    {
        var result = await Task.FromResult(Result.Success(3))
            .BindAsync(x => Task.FromResult(Result.Success(x * 10)));

        result.Value.Should().Be(30);
    }

    [Fact]
    public async Task BindAsync_TaskOverload_Failure_SkipsBinder()
    {
        var called = false;
        var result = await Task.FromResult(Result.Failure<int>("fail"))
            .BindAsync(x =>
            {
                called = true;
                return Task.FromResult(Result.Success(x));
            });

        called.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
    }

    // ─── Match ────────────────────────────────────────────────────────────────

    [Fact]
    public void Match_Success_CallsOnSuccess()
    {
        var result = Result
            .Success(42)
            .Match(onSuccess: v => $"ok:{v}", onFailure: (msg, code) => $"fail:{msg}");

        result.Should().Be("ok:42");
    }

    [Fact]
    public void Match_Failure_CallsOnFailure()
    {
        var result = Result
            .Failure<int>("broke", "CODE")
            .Match(onSuccess: v => $"ok:{v}", onFailure: (msg, code) => $"fail:{msg}|{code}");

        result.Should().Be("fail:broke|CODE");
    }

    // ─── Tap ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Tap_Success_ExecutesActionAndReturnsResult()
    {
        int captured = 0;
        var result = Result.Success(7).Tap(v => captured = v);

        captured.Should().Be(7);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(7);
    }

    [Fact]
    public void Tap_Failure_DoesNotExecuteAction()
    {
        bool called = false;
        var result = Result.Failure<int>("bad").Tap(_ => called = true);

        called.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task TapAsync_Success_AwaitsActionAndReturnsResult()
    {
        int captured = 0;
        var result = await Result
            .Success(99)
            .TapAsync(async v =>
            {
                await Task.Delay(1);
                captured = v;
            });

        captured.Should().Be(99);
        result.Value.Should().Be(99);
    }

    // ─── ToTyped ─────────────────────────────────────────────────────────────

    [Fact]
    public void ToTyped_Failure_ConvertsToTypedFailure()
    {
        var voidResult = Result.Failure("oops", "CODE");
        var typed = voidResult.ToTyped<int>();

        typed.IsFailure.Should().BeTrue();
        typed.ErrorMessage.Should().Be("oops");
        typed.ErrorCode.Should().Be("CODE");
    }

    [Fact]
    public void ToTyped_Success_Throws()
    {
        var voidResult = Result.Success();
        var act = () => voidResult.ToTyped<int>();

        act.Should().Throw<InvalidOperationException>();
    }
}
