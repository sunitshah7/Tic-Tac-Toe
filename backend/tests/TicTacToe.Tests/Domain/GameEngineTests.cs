using TicTacToe.Domain;

namespace TicTacToe.Tests.Domain;

/// <summary>
/// Rules of the game, exercised directly against the pure engine. These cover the
/// "core game logic" list in the specification: valid and invalid moves, turn switching,
/// row/column/diagonal wins, draws, and mode-dependent undo depth.
/// </summary>
public sealed class GameEngineTests
{
    /// <summary>Builds a move list from a sequence of cell indices, alternating X and O.</summary>
    private static List<PlacedMove> Play(params int[] cells) =>
        cells.Select((cell, i) => new PlacedMove(i + 1, GameEngine.PlayerForTurn(i), cell)).ToList();

    [Fact]
    public void NewGame_StartsEmpty_WithXToPlay()
    {
        var snapshot = GameEngine.Evaluate(GameMode.TwoPlayer, Array.Empty<PlacedMove>());

        Assert.Equal(GameStatus.InProgress, snapshot.Status);
        Assert.Equal(Player.X, snapshot.CurrentPlayer);
        Assert.Null(snapshot.Winner);
        Assert.Empty(snapshot.WinningCells);
        Assert.All(snapshot.Board, cell => Assert.Null(cell));
    }

    [Fact]
    public void ValidMove_IsAccepted_AndMarksTheCell()
    {
        var snapshot = GameEngine.Evaluate(GameMode.TwoPlayer, Array.Empty<PlacedMove>());

        Assert.Equal(MoveRejectionReason.None, GameEngine.ValidateMove(snapshot, Player.X, 4));

        var after = GameEngine.Evaluate(GameMode.TwoPlayer, Play(4));
        Assert.Equal(Player.X, after.Board[4]);
        Assert.Single(after.Moves);
    }

    [Fact]
    public void Turns_AlternateAfterEveryValidMove()
    {
        Assert.Equal(Player.X, GameEngine.Evaluate(GameMode.TwoPlayer, Play()).CurrentPlayer);
        Assert.Equal(Player.O, GameEngine.Evaluate(GameMode.TwoPlayer, Play(0)).CurrentPlayer);
        Assert.Equal(Player.X, GameEngine.Evaluate(GameMode.TwoPlayer, Play(0, 1)).CurrentPlayer);
        Assert.Equal(Player.O, GameEngine.Evaluate(GameMode.TwoPlayer, Play(0, 1, 2)).CurrentPlayer);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    [InlineData(100)]
    public void MoveOutsideTheBoard_IsRejected(int cellIndex)
    {
        var snapshot = GameEngine.Evaluate(GameMode.TwoPlayer, Array.Empty<PlacedMove>());

        Assert.Equal(MoveRejectionReason.OutOfBoard, GameEngine.ValidateMove(snapshot, Player.X, cellIndex));
    }

    [Fact]
    public void MoveOnOccupiedCell_IsRejected()
    {
        var snapshot = GameEngine.Evaluate(GameMode.TwoPlayer, Play(4));

        Assert.Equal(MoveRejectionReason.CellOccupied, GameEngine.ValidateMove(snapshot, Player.O, 4));
    }

    [Fact]
    public void MoveByTheWrongPlayer_IsRejected_AndLeavesTheTurnUnchanged()
    {
        var snapshot = GameEngine.Evaluate(GameMode.TwoPlayer, Play(4));

        Assert.Equal(MoveRejectionReason.WrongPlayer, GameEngine.ValidateMove(snapshot, Player.X, 0));
        Assert.Equal(Player.O, snapshot.CurrentPlayer);
    }

    [Fact]
    public void MoveAfterCompletion_IsRejected()
    {
        // X takes the top row.
        var snapshot = GameEngine.Evaluate(GameMode.TwoPlayer, Play(0, 3, 1, 4, 2));

        Assert.Equal(GameStatus.Won, snapshot.Status);
        Assert.Equal(MoveRejectionReason.GameCompleted, GameEngine.ValidateMove(snapshot, Player.O, 5));
    }

    [Fact]
    public void InComputerMode_ClientCannotPlayO()
    {
        var snapshot = GameEngine.Evaluate(GameMode.Computer, Play(0));

        Assert.Equal(Player.O, snapshot.CurrentPlayer);
        Assert.Equal(MoveRejectionReason.NotHumanControlled, GameEngine.ValidateMove(snapshot, Player.O, 4));
    }

    [Theory]
    [InlineData(0, 1, 2)]
    [InlineData(3, 4, 5)]
    [InlineData(6, 7, 8)]
    public void RowWin_IsDetected_WithTheWinningCells(int a, int b, int c)
    {
        var free = Enumerable.Range(0, 9).Where(i => i != a && i != b && i != c).ToArray();
        var snapshot = GameEngine.Evaluate(
            GameMode.TwoPlayer,
            Play(a, free[0], b, free[1], c));

        Assert.Equal(GameStatus.Won, snapshot.Status);
        Assert.Equal(Player.X, snapshot.Winner);
        Assert.Null(snapshot.CurrentPlayer);
        Assert.Equal(new[] { a, b, c }, snapshot.WinningCells);
    }

    [Theory]
    [InlineData(0, 3, 6)]
    [InlineData(1, 4, 7)]
    [InlineData(2, 5, 8)]
    public void ColumnWin_IsDetected_WithTheWinningCells(int a, int b, int c)
    {
        var free = Enumerable.Range(0, 9).Where(i => i != a && i != b && i != c).ToArray();
        var snapshot = GameEngine.Evaluate(
            GameMode.TwoPlayer,
            Play(a, free[0], b, free[1], c));

        Assert.Equal(GameStatus.Won, snapshot.Status);
        Assert.Equal(Player.X, snapshot.Winner);
        Assert.Equal(new[] { a, b, c }, snapshot.WinningCells);
    }

    [Theory]
    [InlineData(0, 4, 8)]
    [InlineData(2, 4, 6)]
    public void DiagonalWin_IsDetected_WithTheWinningCells(int a, int b, int c)
    {
        var free = Enumerable.Range(0, 9).Where(i => i != a && i != b && i != c).ToArray();
        var snapshot = GameEngine.Evaluate(
            GameMode.TwoPlayer,
            Play(a, free[0], b, free[1], c));

        Assert.Equal(GameStatus.Won, snapshot.Status);
        Assert.Equal(Player.X, snapshot.Winner);
        Assert.Equal(new[] { a, b, c }, snapshot.WinningCells);
    }

    [Fact]
    public void OCanWin_Too()
    {
        // X: 0,1,5  O: 3,4,7 -> O takes the middle column? 3,4,5 is a row; use column 1,4,7.
        var moves = Play(0, 1, 2, 4, 3, 7);

        var snapshot = GameEngine.Evaluate(GameMode.TwoPlayer, moves);

        Assert.Equal(GameStatus.Won, snapshot.Status);
        Assert.Equal(Player.O, snapshot.Winner);
        Assert.Equal(new[] { 1, 4, 7 }, snapshot.WinningCells);
    }

    [Fact]
    public void FullBoardWithNoLine_IsADraw()
    {
        // X O X / X O O / O X X - nine cells, no line.
        var snapshot = GameEngine.Evaluate(GameMode.TwoPlayer, Play(0, 1, 2, 4, 3, 5, 7, 6, 8));

        Assert.Equal(GameStatus.Draw, snapshot.Status);
        Assert.Null(snapshot.Winner);
        Assert.Null(snapshot.CurrentPlayer);
        Assert.Empty(snapshot.WinningCells);
        Assert.Equal(9, snapshot.Moves.Count);
    }

    [Fact]
    public void UndoDepth_IsOneMove_InTwoPlayerMode()
    {
        Assert.Equal(0, GameEngine.UndoDepth(GameMode.TwoPlayer, Play()));
        Assert.Equal(1, GameEngine.UndoDepth(GameMode.TwoPlayer, Play(0)));
        Assert.Equal(1, GameEngine.UndoDepth(GameMode.TwoPlayer, Play(0, 4)));
    }

    [Fact]
    public void UndoDepth_IsTheMovePair_InComputerMode()
    {
        // Ends on the computer reply: both come off, returning the turn to X.
        Assert.Equal(2, GameEngine.UndoDepth(GameMode.Computer, Play(0, 4)));

        // Ends on a human move, which only happens when that move finished the game:
        // there is no computer reply to remove.
        Assert.Equal(1, GameEngine.UndoDepth(GameMode.Computer, Play(0, 4, 1)));

        Assert.Equal(0, GameEngine.UndoDepth(GameMode.Computer, Play()));
    }

    [Fact]
    public void ToCellIndex_MapsRowAndColumnRowMajor()
    {
        Assert.Equal(0, GameEngine.ToCellIndex(0, 0));
        Assert.Equal(4, GameEngine.ToCellIndex(1, 1));
        Assert.Equal(8, GameEngine.ToCellIndex(2, 2));
    }
}
