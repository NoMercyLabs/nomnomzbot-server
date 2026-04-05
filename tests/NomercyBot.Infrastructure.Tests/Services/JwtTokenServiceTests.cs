// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NoMercyBot.Infrastructure.Services.Security;

namespace NomercyBot.Infrastructure.Tests.Services;

public class JwtTokenServiceTests
{
    private static JwtTokenService Create(
        string key = "super-secret-key-that-is-at-least-32-bytes-long!",
        string issuer = "TestIssuer",
        string audience = "TestAudience",
        string expirationMinutes = "60")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Key", key },
                { "Jwt:Issuer", issuer },
                { "Jwt:Audience", audience },
                { "Jwt:ExpirationMinutes", expirationMinutes }
            })
            .Build();

        return new JwtTokenService(config);
    }

    // ─── GenerateToken ────────────────────────────────────────────────────────

    [Fact]
    public void GenerateToken_ValidInputs_ReturnsNonEmptyString()
    {
        var svc = Create();
        var token = svc.GenerateToken("uid1", "alice");

        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_HasThreeJwtParts()
    {
        var svc = Create();
        var token = svc.GenerateToken("uid1", "alice");

        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void GenerateToken_DifferentCalls_ProduceDifferentTokens()
    {
        var svc = Create();
        var t1 = svc.GenerateToken("uid1", "alice");
        var t2 = svc.GenerateToken("uid1", "alice");

        // Different JTI claims → different tokens
        t1.Should().NotBe(t2);
    }

    // ─── ValidateToken ────────────────────────────────────────────────────────

    [Fact]
    public void ValidateToken_ValidToken_ReturnsPrincipal()
    {
        var svc = Create();
        var token = svc.GenerateToken("uid1", "alice");

        var principal = svc.ValidateToken(token);

        principal.Should().NotBeNull();
    }

    [Fact]
    public void ValidateToken_ValidToken_ContainsNameIdentifierClaim()
    {
        var svc = Create();
        var token = svc.GenerateToken("uid1", "alice");

        var principal = svc.ValidateToken(token);

        principal!.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be("uid1");
    }

    [Fact]
    public void ValidateToken_ValidToken_ContainsNameClaim()
    {
        var svc = Create();
        var token = svc.GenerateToken("uid1", "alice");

        var principal = svc.ValidateToken(token);

        principal!.FindFirstValue(ClaimTypes.Name).Should().Be("alice");
    }

    [Fact]
    public void ValidateToken_WithRoles_ContainsRoleClaims()
    {
        var svc = Create();
        var token = svc.GenerateToken("uid1", "alice", ["admin", "moderator"]);

        var principal = svc.ValidateToken(token);

        var roles = principal!.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        roles.Should().Contain("admin").And.Contain("moderator");
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        var svc = Create();
        var principal = svc.ValidateToken("not.a.valid.token");

        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_TamperedToken_ReturnsNull()
    {
        var svc = Create();
        var token = svc.GenerateToken("uid1", "alice");
        var parts = token.Split('.');
        var tampered = parts[0] + "." + parts[1] + ".INVALIDSIGNATURE";

        var principal = svc.ValidateToken(tampered);

        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WrongKey_ReturnsNull()
    {
        var svc1 = Create(key: "super-secret-key-that-is-at-least-32-bytes-long!");
        var svc2 = Create(key: "different-key-that-is-at-least-32-bytes-long-x!");

        var token = svc1.GenerateToken("uid1", "alice");
        var principal = svc2.ValidateToken(token);

        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WrongIssuer_ReturnsNull()
    {
        var svc1 = Create(issuer: "Issuer1");
        var svc2 = Create(issuer: "Issuer2");

        var token = svc1.GenerateToken("uid1", "alice");
        var principal = svc2.ValidateToken(token);

        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WrongAudience_ReturnsNull()
    {
        var svc1 = Create(audience: "Audience1");
        var svc2 = Create(audience: "Audience2");

        var token = svc1.GenerateToken("uid1", "alice");
        var principal = svc2.ValidateToken(token);

        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_EmptyString_ReturnsNull()
    {
        var svc = Create();
        var principal = svc.ValidateToken(string.Empty);

        principal.Should().BeNull();
    }
}
