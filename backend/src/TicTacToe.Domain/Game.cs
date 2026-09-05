namespace TicTacToe.Domain;

/// <summary>
/// A persisted game session. It stores only what cannot be derived: identity, mode, the
/// ordered move list, and which result (if any) has already been counted on the
/// scoreboard. Board, turn, status, winner and winning cells come from
/// <see cref="GameEngine.Evaluate"/> on demand.
/// </summary>
public sealed class Game
{
    public required Guid Id { get; init; }

    public required GameMode Mode { get; set; }

    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Moves in play order. Undo truncates from the end.</summary>
    public List<PlacedMove> Moves { get; init; } = new();

    /// <summary>
    /// The result already added to the scoreboard for this game, or null if none has been.
    /// This single field enforces both "update only once for a completed game" and the
    /// Option B rule that a reversed result must be taken back off the scoreboard.
    /// </summary>
    public GameResult? RecordedResult { get; set; }

    /// <summary>Derives the current state of this session.</summary>
    public GameSnapshot Snapshot() => GameEngine.Evaluate(Mode, Moves);

    /// <summary>Clears the board and history, keeping the session id and mode.</summary>
    public void Clear()
    {
        Moves.Clear();
        RecordedResult = null;
    }
}
