namespace TicTacToe.Domain;

/// <summary>
/// The fully derived state of a game at a point in time. Produced by
/// <see cref="GameEngine.Evaluate"/> and never stored: recomputing it from the move
/// list keeps persisted state minimal and guarantees the board can never drift
/// out of sync with the history.
/// </summary>
public sealed record GameSnapshot
{
    public required GameMode Mode { get; init; }

    /// <summary>Moves in play order.</summary>
    public required IReadOnlyList<PlacedMove> Moves { get; init; }

    /// <summary>Nine cells, row-major; null where empty.</summary>
    public required IReadOnlyList<Player?> Board { get; init; }

    /// <summary>Whose turn it is, or null once the game is complete.</summary>
    public required Player? CurrentPlayer { get; init; }

    public required GameStatus Status { get; init; }

    /// <summary>The winning mark, or null when the game is in progress or drawn.</summary>
    public required Player? Winner { get; init; }

    /// <summary>The three cell indices forming the win, or empty when there is no winner.</summary>
    public required IReadOnlyList<int> WinningCells { get; init; }

    /// <summary>True once the game has been decided either way.</summary>
    public bool IsComplete => Status != GameStatus.InProgress;

    /// <summary>
    /// The scoreboard-facing result of this game, or null while it is still in progress.
    /// </summary>
    public GameResult? Result => Status switch
    {
        GameStatus.Won => Winner == Player.X ? GameResult.XWin : GameResult.OWin,
        GameStatus.Draw => GameResult.Draw,
        _ => null
    };
}
