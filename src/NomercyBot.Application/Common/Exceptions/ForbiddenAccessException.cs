namespace NoMercyBot.Application.Common.Exceptions;

/// <summary>
/// Thrown when the current user does not have permission to perform the requested action.
/// </summary>
public sealed class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException()
        : base("You do not have permission to perform this action.")
    {
    }

    public ForbiddenAccessException(string message)
        : base(message)
    {
    }

    public ForbiddenAccessException(string userId, string resource)
        : base($"User '{userId}' does not have access to '{resource}'.")
    {
    }
}
