namespace NoMercyBot.Api.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Secret { get; set; }
    public string Issuer { get; set; } = "NomercyBot";
    public string Audience { get; set; } = "NomercyBot";
    public int ExpiryMinutes { get; set; } = 60;
    public int RefreshExpiryDays { get; set; } = 30;
}
