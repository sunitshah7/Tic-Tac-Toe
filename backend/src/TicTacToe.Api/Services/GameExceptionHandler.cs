using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TicTacToe.Domain;

namespace TicTacToe.Api.Services;

/// <summary>
/// Translates domain rule violations into RFC 7807 problem responses, so controllers can
/// state the happy path and every rejection is shaped identically for the client.
/// </summary>
public sealed class GameExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GameExceptionHandler> _logger;

    public GameExceptionHandler(ILogger<GameExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not GameException gameException)
        {
            return false;
        }

        var status = StatusFor(gameException);
        _logger.LogInformation(
            "Rejected {Method} {Path}: {ErrorCode} - {Message}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            gameException.ErrorCode,
            gameException.Message);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = status == StatusCodes.Status404NotFound ? "Not found" : "Invalid game operation",
            Detail = gameException.Message,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["errorCode"] = gameException.ErrorCode;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    /// <summary>
    /// A malformed request is a 400; a well-formed request that the current game state
    /// forbids is a 409, which lets the client tell "you asked wrongly" from "you asked at
    /// the wrong time".
    /// </summary>
    private static int StatusFor(GameException exception) => exception switch
    {
        GameNotFoundException => StatusCodes.Status404NotFound,
        InvalidMoveRequestException => StatusCodes.Status400BadRequest,
        MoveRejectedException { Reason: MoveRejectionReason.OutOfBoard } => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status409Conflict
    };
}
