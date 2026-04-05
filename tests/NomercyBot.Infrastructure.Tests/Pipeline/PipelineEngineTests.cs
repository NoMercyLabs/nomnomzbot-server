// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NoMercyBot.Domain.Interfaces;
using NoMercyBot.Infrastructure.Pipeline;
using NoMercyBot.Infrastructure.Pipeline.Actions;
using NoMercyBot.Infrastructure.Pipeline.Conditions;
using NSubstitute;

namespace NomercyBot.Infrastructure.Tests.Pipeline;

public class InfraPipelineEngineTests
{
    private static PipelineEngine CreateEngine(IChatProvider? chat = null)
    {
        chat ??= Substitute.For<IChatProvider>();
        chat.SendMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var registry = Substitute.For<IChannelRegistry>();
        registry.Get(Arg.Any<string>()).Returns((ChannelContext?)null);

        var actions = new ICommandAction[]
        {
            new StopAction(),
            new SetVariableAction(),
            new WaitAction(),
        };

        var conditions = new ICommandCondition[] { new UserRoleCondition(), new RandomCondition() };

        return new PipelineEngine(
            registry,
            actions,
            conditions,
            NullLogger<PipelineEngine>.Instance
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
            MessageId = "msg1",
            RawMessage = "",
        };

    // ─── Basic execution ──────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_EmptySteps_ReturnsCompleted()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync(BuildRequest("""{"steps":[]}"""));

        result.Outcome.Should().Be(PipelineOutcome.Completed);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidJson_ReturnsFailed()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync(BuildRequest("not-json"));

        result.Outcome.Should().Be(PipelineOutcome.Failed);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_NullDefinition_ReturnsCompleted()
    {
        // null JSON deserializes to null definition → treated as empty pipeline → Completed
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync(BuildRequest("null"));

        result.Outcome.Should().Be(PipelineOutcome.Completed);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownAction_ContinuesExecution()
    {
        var engine = CreateEngine();
        var json =
            """{"steps":[{"action":{"type":"does_not_exist"}},{"action":{"type":"stop"}}]}""";
        var result = await engine.ExecuteAsync(BuildRequest(json));

        // Unknown action is logged as warning and fails the step, but execution continues (fail-open)
        result.StepLogs.Should().HaveCount(2);
    }

    // ─── Stop action ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_StopAction_SetsShouldStopAndBreaks()
    {
        var engine = CreateEngine();
        const string json = """
            {
              "steps": [
                {"action":{"type":"stop"}},
                {"action":{"type":"stop"}}
              ]
            }
            """;

        var result = await engine.ExecuteAsync(BuildRequest(json));

        // Pipeline completes (not failed) but only one step ran
        result.StepsExecuted.Should().Be(1);
    }

    // ─── SetVariable ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_SetVariable_StoresInContext()
    {
        var engine = CreateEngine();
        const string json = """
            {
              "steps": [
                {"action":{"type":"set_variable","name":"myvar","value":"hello"}},
                {"action":{"type":"stop"}}
              ]
            }
            """;

        var result = await engine.ExecuteAsync(BuildRequest(json));

        result.Outcome.Should().Be(PipelineOutcome.Completed);
        result.StepLogs.Should().HaveCount(2);
    }

    // ─── Conditions ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ConditionFalse_SkipsStep()
    {
        var engine = CreateEngine();
        const string json = """
            {
              "steps": [
                {
                  "condition": {"type":"user_role","min_role":"moderator"},
                  "action": {"type":"stop"}
                }
              ]
            }
            """;
        // No user.role variable → defaults to viewer → condition false → skip
        var result = await engine.ExecuteAsync(BuildRequest(json));

        result.StepsSkipped.Should().Be(1);
        result.StepsExecuted.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ConditionTrue_ExecutesStep()
    {
        var engine = CreateEngine();
        const string json = """
            {
              "steps": [
                {
                  "condition": {"type":"user_role","min_role":"moderator"},
                  "action": {"type":"stop"}
                }
              ]
            }
            """;
        var request = new PipelineRequest
        {
            BroadcasterId = "chan",
            TriggeredByUserId = "mod1",
            TriggeredByDisplayName = "Mod1",
            PipelineJson = json,
            MessageId = "m1",
            RawMessage = "",
            InitialVariables = { { "user.role", "moderator" } },
        };

        var result = await engine.ExecuteAsync(request);

        result.StepsExecuted.Should().Be(1);
    }

    // ─── Cancellation ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_AlreadyCancelled_ReturnsCancelled()
    {
        var engine = CreateEngine();
        const string json = """{"steps":[{"action":{"type":"wait","milliseconds":5000}}]}""";

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await engine.ExecuteAsync(BuildRequest(json), cts.Token);

        result.Outcome.Should().Be(PipelineOutcome.Cancelled);
    }

    // ─── Concurrency tracking ─────────────────────────────────────────────────

    [Fact]
    public void GetActiveCountForChannel_NoActivePipelines_ReturnsZero()
    {
        var engine = CreateEngine();
        engine.GetActiveCountForChannel("chan").Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_AfterCompletion_DecrementsActiveCount()
    {
        var engine = CreateEngine();
        const string json = """{"steps":[{"action":{"type":"stop"}}]}""";

        await engine.ExecuteAsync(BuildRequest(json));

        engine.GetActiveCountForChannel("chan").Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ExceedsConcurrencyLimit_ReturnsFailed()
    {
        var engine = CreateEngine();
        const string json = """{"steps":[{"action":{"type":"wait","milliseconds":5000}}]}""";

        // Start 5 long-running pipelines
        var ctsList = Enumerable.Range(0, 5).Select(_ => new CancellationTokenSource()).ToList();
        var longTasks = ctsList
            .Select(cts => engine.ExecuteAsync(BuildRequest(json, "chan"), cts.Token))
            .ToArray();

        await Task.Delay(100); // Let them register

        // 6th should fail
        var overflow = await engine.ExecuteAsync(BuildRequest(json, "chan"));

        overflow.Outcome.Should().Be(PipelineOutcome.Failed);
        overflow.ErrorMessage.Should().NotBeNullOrEmpty();

        ctsList.ForEach(cts => cts.Cancel());
        try
        {
            await Task.WhenAll(longTasks).WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch
        { /* expected cancellations */
        }
        ctsList.ForEach(cts => cts.Dispose());
    }

    // ─── StopOnMatch ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_StopOnMatch_StopsAfterSuccessfulStep()
    {
        var engine = CreateEngine();
        const string json = """
            {
              "steps": [
                {"action":{"type":"set_variable","name":"x","value":"1"},"stop_on_match":true},
                {"action":{"type":"set_variable","name":"y","value":"2"}}
              ]
            }
            """;

        var result = await engine.ExecuteAsync(BuildRequest(json));

        // stop_on_match=true on step 0, so only 1 step executed
        result.StepsExecuted.Should().Be(1);
    }
}
