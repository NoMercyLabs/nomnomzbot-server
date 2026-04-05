using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NoMercyBot.Application.Common.Interfaces;

namespace NoMercyBot.Infrastructure.Services.Security;

/// <summary>
/// Generates and validates JWT tokens for API authentication.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly byte[] _key;
    private readonly TimeSpan _expiration;

    public JwtTokenService(IConfiguration configuration)
    {
        IConfigurationSection jwtSection = configuration.GetSection("Jwt");
        _issuer = jwtSection["Issuer"] ?? "NomercyBot";
        _audience = jwtSection["Audience"] ?? "NomercyBot";
        _key = Encoding.UTF8.GetBytes(
            jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key is not configured.")
        );
        _expiration = TimeSpan.FromMinutes(double.Parse(jwtSection["ExpirationMinutes"] ?? "60"));
    }

    public string GenerateToken(string userId, string username, IEnumerable<string>? roles = null)
    {
        List<Claim> claims = new()
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(
                JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            ),
        };

        if (roles is not null)
        {
            foreach (string role in roles)
            {
                claims.Add(new(ClaimTypes.Role, role));
            }
        }

        SymmetricSecurityKey securityKey = new(_key);
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(_expiration),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        JwtSecurityTokenHandler tokenHandler = new();
        TokenValidationParameters validationParameters = new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(_key),
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };

        try
        {
            return tokenHandler.ValidateToken(token, validationParameters, out _);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
