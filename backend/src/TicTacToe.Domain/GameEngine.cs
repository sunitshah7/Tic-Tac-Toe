namespace TicTacToe.Domain;

/// <summary>
/// The rules of Tic Tac Toe, as pure functions over a move list. This type holds no
/// state and touches no infrastructure, so every rule in the specification is unit
/// testable in isolation and the API layer stays a thin translation of HTTP to rules.
/// </summary>
public static class GameEngine
{
    /// <summary>Number of cells on a standard board.</summary>
    public const int CellCount = 9;

    /// <summary>Board edge length.</summary>
    public const int Size = 3;

    /// <summary>X always opens.</summary>
    public const Player StartingPlayer = Player.X;

    /// <summary>The eight winning lines: three rows, three columns, two diagonals.</summary>
    public static readonly IReadOnlyList<int[]> WinningLines = new[]
    {
        new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 }, // rows
        new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 }, // columns
        new[] { 0, 4, 8 }, new[] { 2, 4, 6 }                     // diagonals
    };

    /// <summary>Translates a 0-based row and column into a flat cell index.</summary>
    public static int ToCellIndex(int row, int column) => (row * Size) + column;

    /// <summary>True when a flat index addresses a real cell.</summary>
    public static bool IsOnBoard(int cellIndex) => cellIndex >= 0 && cellIndex < CellCount;

    /// <summary>The mark that plays the move after <paramref name="movesPlayed"/> moves.</summary>
    public static Player PlayerForTurn(int movesPlayed) =>
        movesPlayed % 2 == 0 ? Player.X : Player.O;

    /// <summary>The other mark.</summary>
    public static Player Opponent(Player player) => player == Player.X ? Player.O : Player.X;

    /// <summary>
    /// Derives the complete state of a game from its ordered move list. The move list is
    /// assumed to already be legal — it is only ever produced by <see cref="ValidateMove"/>.
    /// </summary>
    public static GameSnapshot Evaluate(GameMode mode, IReadOnlyList<PlacedMove> moves)
    {
        ArgumentNullException.ThrowIfNull(moves);

        var ordered = moves.OrderBy(m => m.MoveNumber).ToList();
        var board = BuildBoard(ordered);

        var (winner, winningCells) = FindWinner(board);
        if (winner is not null)
        {
            return new GameSnapshot
            {
                Mode = mode,
                Moves = ordered,
                Board = board,
                CurrentPlayer = null,
                Status = GameStatus.Won,
                Winner = winner,
                WinningCells = winningCells
            };
        }

        var isDraw = ordered.Count == CellCount;
        return new GameSnapshot
        {
            Mode = mode,
            Moves = ordered,
            Board = board,
            CurrentPlayer = isDraw ? null : PlayerForTurn(ordered.Count),
            Status = isDraw ? GameStatus.Draw : GameStatus.InProgress,
            Winner = null,
            WinningCells = Array.Empty<int>()
        };
    }

    /// <summary>Replays the moves onto an empty board.</summary>
    public static Player?[] BuildBoard(IEnumerable<PlacedMove> moves)
    {
        var board = new Player?[CellCount];
        foreach (var move in moves)
        {
            board[move.CellIndex] = move.Player;
        }

        return board;
    }

    /// <summary>
    /// Finds a completed line, returning the winning mark and the three cells that formed it.
    /// </summary>
    public static (Player? Winner, IReadOnlyList<int> Cells) FindWinner(IReadOnlyList<Player?> board)
    {
        ArgumentNullException.ThrowIfNull(board);

        foreach (var line in WinningLines)
        {
            var first = board[line[0]];
            if (first is not null && board[line[1]] == first && board[line[2]] == first)
            {
                return (first, line);
            }
        }

        return (null, Array.Empty<int>());
    }

    /// <summary>
    /// Checks a proposed move against every rule the specification calls out. Returns
    /// <see cref="MoveRejectionReason.None"/> when the move may be applied.
    /// </summary>
    public static MoveRejectionReason ValidateMove(GameSnapshot snapshot, Player player, int cellIndex)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!IsOnBoard(cellIndex))
        {
            return MoveRejectionReason.OutOfBoard;
        }

        if (snapshot.IsComplete)
        {
            return MoveRejectionReason.GameCompleted;
        }

        // In computer mode O belongs to the engine; a client may only submit X.
        if (snapshot.Mode == GameMode.Computer && player == Player.O)
        {
            return MoveRejectionReason.NotHumanControlled;
        }

        if (player != snapshot.CurrentPlayer)
        {
            return MoveRejectionReason.WrongPlayer;
        }

        if (snapshot.Board[cellIndex] is not null)
        {
            return MoveRejectionReason.CellOccupied;
        }

        return MoveRejectionReason.None;
    }

    /// <summary>
    /// Appends a move to the sequence. Callers must have validated it first.
    /// </summary>
    public static PlacedMove NextMove(IReadOnlyList<PlacedMove> moves, Player player, int cellIndex) =>
        new(moves.Count + 1, player, cellIndex);

    /// <summary>
    /// How many trailing moves a single Undo removes, per the mode-specific behaviour in
    /// the specification. Two Player mode steps back one move. Computer mode steps back the
    /// engine's reply together with the human move that provoked it, so control returns to
    /// the human — but when the human's own move ended the game the engine never replied,
    /// and only that one move comes off.
    /// </summary>
    public static int UndoDepth(GameMode mode, IReadOnlyList<PlacedMove> moves)
    {
        ArgumentNullException.ThrowIfNull(moves);

        if (moves.Count == 0)
        {
            return 0;
        }

        if (mode == GameMode.TwoPlayer)
        {
            return 1;
        }

        var lastWasComputer = moves[^1].Player == Player.O;
        return lastWasComputer ? Math.Min(2, moves.Count) : 1;
    }
}
