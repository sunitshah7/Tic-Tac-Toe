using TicTacToe.Domain;

namespace TicTacToe.Api.Contracts;

/// <summary>One row of the move history.</summary>
/// <param name="MoveNumber">1-based ordinal of the move.</param>
/// <param name="Player">The mark that was placed.</param>
/// <param name="Row">0-based row.</param>
/// <param name="Column">0-based column.</param>
/// <param name="CellIndex">0-based flat index.</param>
/// <param name="Position">
/// Display string in the form the specification's example uses, e.g. "Row 1, Column 1".
/// Formatted here so every client renders the history identically.
/// </param>
public sealed record MoveResponse(
    int MoveNumber,
    Player Player,
    int Row,
    int Column,
    int CellIndex,
    string Position);

/// <summary>Session-level tallies.</summary>
public sealed record ScoreboardResponse(int XWins, int OWins, int Draws);

/// <summary>
/// Everything the frontend needs to render the game. The backend is the source of truth,
/// so every mutating endpoint returns this same shape and the client simply re-renders it.
/// </summary>
public sealed record GameStateResponse
{
    /// <summary>Session identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Two Player or Computer.</summary>
    public required GameMode Mode { get; init; }

    /// <summary>Nine cells, row-major; null where empty.</summary>
    public required IReadOnlyList<Player?> Board { get; init; }

    /// <summary>Whose turn it is, or null once the game is complete.</summary>
    public required Player? CurrentPlayer { get; init; }

    /// <summary>InProgress, Won or Draw.</summary>
    public required GameStatus Status { get; init; }

    /// <summary>The winning mark, when there is one.</summary>
    public required Player? Winner { get; init; }

    /// <summary>The three cells to highlight, when there is a winner.</summary>
    public required IReadOnlyList<int> WinningCells { get; init; }

    /// <summary>Move history for the current game, in play order.</summary>
    public required IReadOnlyList<MoveResponse> Moves { get; init; }

    /// <summary>Whether Undo is available; drives the disabled state of the button.</summary>
    public required bool CanUndo { get; init; }

    /// <summary>
    /// How many moves the next Undo would remove, given the mode. One in Two Player mode;
    /// normally two in Computer mode.
    /// </summary>
    public required int UndoDepth { get; init; }

    /// <summary>Current scoreboard, embedded so the client needs a single round trip.</summary>
    public required ScoreboardResponse Scoreboard { get; init; }
}
