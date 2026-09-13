using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Merito.Server.Infrastructure;

/// <summary>Turns domain refusals and lost concurrency races into problem details with a readable message.</summary>
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, message) = exception switch
        {
            DomainException d => (d.Error switch
            {
                DomainError.Validation => StatusCodes.Status400BadRequest,
                DomainError.NotFound => StatusCodes.Status404NotFound,
                DomainError.Forbidden => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status409Conflict,
            }, d.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
                "Данные уже изменились. Обновите экран и попробуйте еще раз."),
            _ => (0, ""),
        };
        if (status == 0) return false;

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = message, Detail = message },
        });
    }
}
