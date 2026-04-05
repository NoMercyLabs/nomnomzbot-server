// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentAssertions;
using NoMercyBot.Domain.Exceptions;

namespace NomercyBot.Domain.Tests.Exceptions;

public class DomainExceptionTests
{
    [Fact]
    public void DomainException_WithMessage_SetsMessage()
    {
        var ex = new DomainException("something went wrong");
        ex.Message.Should().Be("something went wrong");
    }

    [Fact]
    public void DomainException_WithMessageAndInner_SetsMessageAndInner()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new DomainException("outer", inner);

        ex.Message.Should().Be("outer");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void DomainException_IsExceptionSubclass()
    {
        var ex = new DomainException("test");
        ex.Should().BeAssignableTo<Exception>();
    }
}

public class CooldownActiveExceptionTests
{
    [Fact]
    public void CooldownActiveException_SetsRemainingCooldown()
    {
        var remaining = TimeSpan.FromSeconds(15);
        var ex = new CooldownActiveException("!so", remaining);

        ex.RemainingCooldown.Should().Be(remaining);
    }

    [Fact]
    public void CooldownActiveException_MessageContainsCommandName()
    {
        var ex = new CooldownActiveException("!uptime", TimeSpan.FromSeconds(30));
        ex.Message.Should().Contain("!uptime");
    }

    [Fact]
    public void CooldownActiveException_MessageContainsSeconds()
    {
        var ex = new CooldownActiveException("!so", TimeSpan.FromSeconds(45));
        ex.Message.Should().Contain("45");
    }

    [Fact]
    public void CooldownActiveException_IsDomainException()
    {
        var ex = new CooldownActiveException("!cmd", TimeSpan.FromSeconds(5));
        ex.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void CooldownActiveException_ZeroRemaining_StillCreates()
    {
        var ex = new CooldownActiveException("!cmd", TimeSpan.Zero);
        ex.RemainingCooldown.Should().Be(TimeSpan.Zero);
    }
}

public class CommandNotFoundExceptionTests
{
    [Fact]
    public void CommandNotFoundException_MessageContainsCommandName()
    {
        var ex = new CommandNotFoundException("!mystery");
        ex.Message.Should().Contain("!mystery");
    }

    [Fact]
    public void CommandNotFoundException_IsDomainException()
    {
        var ex = new CommandNotFoundException("!cmd");
        ex.Should().BeAssignableTo<DomainException>();
    }
}

public class EntityNotFoundExceptionTests
{
    [Fact]
    public void EntityNotFoundException_SetsEntityTypeAndId()
    {
        var ex = new EntityNotFoundException("Channel", "ch123");

        ex.EntityType.Should().Be("Channel");
        ex.EntityId.Should().Be("ch123");
    }

    [Fact]
    public void EntityNotFoundException_MessageContainsTypeAndId()
    {
        var ex = new EntityNotFoundException("User", "user-456");

        ex.Message.Should().Contain("User");
        ex.Message.Should().Contain("user-456");
    }

    [Fact]
    public void EntityNotFoundException_WithInnerException_SetsInner()
    {
        var inner = new Exception("db error");
        var ex = new EntityNotFoundException("Command", "cmd-1", inner);

        ex.InnerException.Should().BeSameAs(inner);
        ex.EntityType.Should().Be("Command");
        ex.EntityId.Should().Be("cmd-1");
    }

    [Fact]
    public void EntityNotFoundException_IsDomainException()
    {
        var ex = new EntityNotFoundException("Channel", "x");
        ex.Should().BeAssignableTo<DomainException>();
    }
}

public class InsufficientPermissionExceptionTests
{
    [Fact]
    public void InsufficientPermissionException_MessageContainsAction()
    {
        var ex = new InsufficientPermissionException("delete channel");
        ex.Message.Should().Contain("delete channel");
    }

    [Fact]
    public void InsufficientPermissionException_IsDomainException()
    {
        var ex = new InsufficientPermissionException("some action");
        ex.Should().BeAssignableTo<DomainException>();
    }
}
