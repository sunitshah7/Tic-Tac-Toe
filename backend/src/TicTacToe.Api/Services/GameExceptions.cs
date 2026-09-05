using TicTacToe.Domain;

namespace TicTacToe.Api.Services;

/// <summary>Base type for rule violations the API translates into HTTP problem responses.</summary>
public abstract class GameException : Exception
{
    protected GameException(string message)
        : base(message)
    {
    }

    /// <summary>Stable machine-readable code, surfaced to clients as <c>errorCode</c>.</summary>
    public abstract string ErrorCode { get; }
}

/// <summary>The requested game session does not exist.</summary>
public sealed class GameNotFoundException : GameException
{
    public GameNotFoundException(Guid id)
        : base($"Game '{id}' was not found.")
    {
    }

    public override string ErrorCode => "GameNotFound";
}

/// <summary>The move broke one of the rules in <see cref="GameEngine.ValidateMove"/>.</summary>
public sealed class MoveRejectedException : GameException
{
    public MoveRejectedException(MoveRejectionReason reason)
        : base(DescribeReason(reason))
    {
        Reason = reason;
    }

    public MoveRejectionReason Reason { get; }

    public override string ErrorCode => Reason.ToString();

    private static string DescribeReason(MoveRejectionReason reason) => reason switch
    {
        MoveRejectionReason.OutOfBoard => "The requested cell is outside the 3x3 board.",
        MoveRejectionReason.CellOccupied => "That cell is already taken.",
        MoveRejectionReason.GameCompleted => "The game is already complete; no further moves are accepted.",
        MoveRejectionReason.WrongPlayer => "It is not that player's turn.",
        MoveRejectionReason.NotHumanControlled => "In computer mode the human plays X; O is played by the backend.",
        _ => "The move is not valid."
    };
}

/// <summary>Undo was requested with no moves left to take back.</summary>
public sealed class UndoNotAvailableException : GameException
{
    public UndoNotAvailableException()
        : base("There are no moves to undo.")
    {
    }

    public override string ErrorCode => "NothingToUndo";
}

/// <summary>The move request identified neither a cell index nor a row and column.</summary>
public sealed class InvalidMoveRequestException : GameException
{
    public InvalidMoveRequestException(string message)
        : base(message)
    {
    }

    public override string ErrorCode => "InvalidMoveRequest";
}
