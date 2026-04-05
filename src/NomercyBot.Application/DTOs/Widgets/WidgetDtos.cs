namespace NoMercyBot.Application.DTOs.Widgets;

public sealed record WidgetListItem(
    string Id,
    string Name,
    string Type,
    bool IsEnabled,
    DateTime CreatedAt);

public sealed record WidgetDetail(
    string Id,
    string Name,
    string Type,
    bool IsEnabled,
    string? OverlayUrl,
    Dictionary<string, object?> Settings,
    List<string> EventSubscriptions,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateWidgetRequest
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public Dictionary<string, object?>? Settings { get; init; }
    public List<string>? EventSubscriptions { get; init; }
}

public sealed record UpdateWidgetRequest
{
    public string? Name { get; init; }
    public Dictionary<string, object?>? Settings { get; init; }
    public List<string>? EventSubscriptions { get; init; }
    public bool? IsEnabled { get; init; }
}
