// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NoMercyBot.Application.Pipeline;
using NoMercyBot.Application.Pipeline.Actions;
using NoMercyBot.Application.Pipeline.Conditions;
using NoMercyBot.Application.Services.Pipeline;
using NoMercyBot.Domain.Interfaces;
using NSubstitute;

// ReSharper disable InconsistentNaming

namespace NomNomzBot.Application.Tests.Pipeline;

public class PipelineEngineTests
{
    private static PipelineEngine CreateEngine(params ICommandAction[] extraActions)
    {
        IChatProvider? chat = Substitute.For<IChatProvider>();
        chat.SendMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        CommandActionRegistry actionRegistry = new();
        ConditionEvaluatorRegistry conditionRegistry = new();
        NullLogger<PipelineEngine> logger = NullLogger<PipelineEngine>.Instance;

        // Core actions
        List<ICommandAction> actions = new()
        {
            new StopAction(),
            new SetVariableAction(),
            new DelayAction(),
            new SendMessageAction(chat),
        };
        actions.AddRange(extraActions);

        return new(
            actionRegistry,
            conditionRegistry,
            actions,
            [new VariableEqualsCondition()],
            logger
        );
    }

    private static PipelineRequest BuildRequest(
        string json,
        string broadcaster = "chan",
        string user = "user1"
    ) =>
        new()
        {
            BroadcasterId = broadcaster,
            TriggeredByUserId = user,
            TriggeredByDisplayName = "TestUser",
            PipelineJson = json,
        };

    // ─── Happy path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_EmptySteps_ReturnsCompleted()
    {
        PipelineEngine engine = CreateEngine();
        PipelineExecutionResult result = await engine.ExecuteAsync(BuildRequest("""{"steps":[]}"""));

        result.Outcome.Should().Be(PipelineOutcome.Completed);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidJson_ReturnsFailed()
    {
        PipelineEngine engine = CreateEngine();
        PipelineExecutionResult result = await engine.ExecuteAsync(BuildRequest("not-json"));

        result.Outcome.Should().Be(PipelineOutcome.Failed);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_SetVariable_StoresThenAvailable()
    {
        PipelineEngine engine = CreateEngine();
        const string json = """
            {
              "steps": [
                { "action": "set_variable", "params": { "name": "greeting", "value": "Hello" } },
                { "action": "set_variable", "params": { "name": "copy", "value": "{{greeting}}" } }
              ]
            }
            """;

        PipelineExecutionResult result = await engine.ExecuteAsync(BuildRequest(json));

        result.Outcome.Should().Be(PipelineOutcome.Completed);
        result.StepsExecuted.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_StopAction_ReturnsStoppedOutcome()
    {
        PipelineEngine engine = CreateEngine();
        const string json = """
            {
              "steps": [
                { "action": "stop" },
                { "action": "stop" }
              ]
            }
            """;

        PipelineExecutionResult result = await engine.ExecuteAsync(BuildRequest(json));

        result.Outcome.Should().Be(PipelineOutcome.Stopped);
        result.StepsExecuted.Should().Be(1);
    }

    // ─── Condition skipping ──────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ConditionFalse_SkipsStep()
    {
        PipelineEngine engine = CreateEngine();
        const string json = """
            {
              "steps": [
                {
                  "action": "stop",
                  "condition": { "type": "variable_equals", "variable": "x", "value": "99" }
                }
              ]
            }
            """;

        PipelineExecutionResult result = await engine.ExecuteAsync(BuildRequest(json));

        result.Outcome.Should().Be(PipelineOutcome.Completed);
        result.StepsSkipped.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ConditionTrue_ExecutesStep()
    {
        PipelineEngine engine = CreateEngine();
        const string json = """
            {
              "steps": [
                { "action": "set_variable", "params": { "name": "x", "value": "42" } },
                {
                  "action": "stop",
                  "condition": { "type": "variable_equals", "variable": "x", "value": "42" }
                }
              ]
            }
            """;

        PipelineExecutionResult result = await engine.ExecuteAsync(BuildRequest(json));

        result.Outcome.Should().Be(PipelineOutcome.Stopped);
    }

    // ─── Unknown action ──────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_UnknownAction_ReturnsFailed()
    {
        PipelineEngine engine = CreateEngine();
        const string json = """{"steps":[{"action":"does_not_exist"}]}""";

        PipelineExecutionResult result = await engine.ExecuteAsync(BuildRequest(json));

        result.Outcome.Should().Be(PipelineOutcome.Failed);
    }

    // ─── Cancellation ────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_CancelledBeforeStart_ReturnsCancelled()
    {
        PipelineEngine engine = CreateEngine();
        const string json = """{"steps":[{"action":"delay","params":{"seconds":30}}]}""";

        using CancellationTokenSource cts = new();
        cts.Cancel();

        PipelineExecutionResult result = await engine.ExecuteAsync(BuildRequest(json), cts.Token);

        result.Outcome.Should().Be(PipelineOutcome.Cancelled);
    }

    // ─── Built-in variables ──────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_BuiltInVariables_AreSeeded()
    {
        string? sentMessage = null;
        IChatProvider? chat = Substitute.For<IChatProvider>();
        chat.SendMessageAsync(
                Arg.Any<string>(),
                Arg.Do<string>(m => sentMessage = m),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.CompletedTask);

        CommandActionRegistry actionRegistry = new();
        ConditionEvaluatorRegistry conditionRegistry = new();

        PipelineEngine engine = new(
            actionRegistry,
            conditionRegistry,
            [
                new SendMessageAction(chat),
                new StopAction(),
                new SetVariableAction(),
                new DelayAction(),
            ],
            [new VariableEqualsCondition()],
            NullLogger<PipelineEngine>.Instance
        );

        const string json =
            """{"steps":[{"action":"send_message","params":{"message":"Hello {{user}}"}}]}""";
        PipelineRequest request = new()
        {
            BroadcasterId = "chan",
            TriggeredByUserId = "uid1",
            TriggeredByDisplayName = "BobUser",
            PipelineJson = json,
        };

        PipelineExecutionResult result = await engine.ExecuteAsync(request);

        result.Outcome.Should().Be(PipelineOutcome.Completed);
        sentMessage.Should().Be("Hello BobUser");
    }

    // ─── Concurrency limit ───────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ExceedsConcurrencyLimit_ReturnsFailed()
    {
        PipelineEngine engine = CreateEngine();
        const string json = """{"steps":[{"action":"delay","params":{"seconds":30}}]}""";

        // Keep CTSes alive for the duration of the test
        List<CancellationTokenSource> ctsList = Enumerable.Range(0, 5).Select(_ => new CancellationTokenSource()).ToList();

        Task<PipelineExecutionResult>[] longTasks = ctsList
            .Select(cts => engine.ExecuteAsync(BuildRequest(json), cts.Token))
            .ToArray();

        // Give pipelines time to register in the concurrency dictionary
        await Task.Delay(200);

        // This 6th one should be rejected immediately
        PipelineExecutionResult overflow = await engine.ExecuteAsync(BuildRequest(json));

        overflow.Outcome.Should().Be(PipelineOutcome.Failed);
        overflow.ErrorMessage.Should().Contain("concurrency");

        // Cancel all running pipelines to clean up
        ctsList.ForEach(cts => cts.Cancel());
        await engine.CancelAllForChannelAsync("chan");
        try
        {
            await Task.WhenAll(longTasks).WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch
        { /* expected cancellations */
        }

        ctsList.ForEach(cts => cts.Dispose());
    }
}
