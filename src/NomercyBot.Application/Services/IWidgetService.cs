using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Widgets;

namespace NoMercyBot.Application.Services;

/// <summary>
/// Application service for managing overlay widgets (alerts, chat overlays, goals, etc.).
/// </summary>
public interface IWidgetService
{
    /// <summary>Create a new widget.</summary>
    Task<Result<WidgetDetail>> CreateAsync(
        string broadcasterId,
        CreateWidgetRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>Update an existing widget.</summary>
    Task<Result<WidgetDetail>> UpdateAsync(
        string broadcasterId,
        string widgetId,
        UpdateWidgetRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>Delete a widget.</summary>
    Task<Result> DeleteAsync(
        string broadcasterId,
        string widgetId,
        CancellationToken cancellationToken = default
    );

    /// <summary>List all widgets for a channel with pagination.</summary>
    Task<Result<PagedList<WidgetDetail>>> ListAsync(
        string broadcasterId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default
    );

    /// <summary>Get a single widget by ID.</summary>
    Task<Result<WidgetDetail>> GetAsync(
        string broadcasterId,
        string widgetId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Get a widget by its public access token (for overlay URLs).</summary>
    Task<Result<WidgetDetail>> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default
    );
}
