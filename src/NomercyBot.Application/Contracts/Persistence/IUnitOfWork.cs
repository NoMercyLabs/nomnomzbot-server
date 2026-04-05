namespace NoMercyBot.Application.Contracts.Persistence;

/// <summary>
/// Unit of Work pattern abstraction. Wraps SaveChanges to allow the Application layer
/// to commit transactional changes without depending on EF Core directly.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persist all pending changes to the database.
    /// Returns the number of state entries written.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
