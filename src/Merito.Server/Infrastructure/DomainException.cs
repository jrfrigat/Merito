namespace Merito.Server.Infrastructure;

/// <summary>How a rejected request maps onto an HTTP status.</summary>
public enum DomainError
{
    /// <summary>The input breaks a rule (400).</summary>
    Validation,

    /// <summary>The target does not exist or is not visible to the caller (404).</summary>
    NotFound,

    /// <summary>The caller's role does not allow the action (403).</summary>
    Forbidden,

    /// <summary>The state changed under the caller or forbids the action now (409).</summary>
    Conflict,
}

/// <summary>A business rule refused the request; the message is shown to the user as is.</summary>
public sealed class DomainException(DomainError error, string message) : Exception(message)
{
    /// <summary>The kind of refusal.</summary>
    public DomainError Error { get; } = error;

    /// <summary>Creates a 400 refusal.</summary>
    public static DomainException Invalid(string message) => new(DomainError.Validation, message);

    /// <summary>Creates a 404 refusal.</summary>
    public static DomainException NotFound(string message) => new(DomainError.NotFound, message);

    /// <summary>Creates a 403 refusal.</summary>
    public static DomainException Forbidden(string message) => new(DomainError.Forbidden, message);

    /// <summary>Creates a 409 refusal.</summary>
    public static DomainException Conflict(string message) => new(DomainError.Conflict, message);
}
