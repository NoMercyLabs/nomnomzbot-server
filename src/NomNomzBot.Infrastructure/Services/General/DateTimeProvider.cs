using NoMercyBot.Application.Common.Interfaces;

namespace NoMercyBot.Infrastructure.Services.General;

/// <summary>
/// IDateTimeProvider implementation returning UTC times.
/// Abstracts DateTime.UtcNow for testability.
/// </summary>
public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;
}
