using TicTacToe.Api.Contracts;
using TicTacToe.Api.Services;
using TicTacToe.Domain;

namespace TicTacToe.Tests.Services;

/// <summary>
/// State transitions end to end through the application service and SQLite: move
/// application, the automatic computer reply, mode-dependent undo, reset, and the
/// scoreboard bookkeeping that ties them together.
/// </summary>
public sealed class GameServiceTests : IDisposable
{
    private readonly GameServiceHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    private Task<GameStateResponse> CreateAsync(GameMode mode = GameMode.TwoPlayer) =>
        _harness.RunAsync(s => s.CreateGameAsync(mode));

    private Task<GameStateResponse> MoveAsync(Guid id, Player player, int cell) =>
        _harness.RunAsync(s => s.SubmitMoveAsync(id, player, cell));

    /// <summary>Plays an alternating sequence of cells, starting with X.</summary>
    private async Task<GameStateResponse> PlayAsync(Guid id, params int[] cells)
    {
        GameStateResponse? state = null;
        for (var i = 0; i < cells.Length; i++)
        {
            state = await MoveAsync(id, GameEngine.PlayerForTurn(i), cells[i]);
        }

        return state ?? await _harness.RunAsync(s => s.GetGameAsync(id));
    }

    /// <summary>X takes the top row in five moves.</summary>
    private Task<GameStateResponse> PlayXWinAsync(Guid id) => PlayAsync(id, 0, 3, 1, 4, 2);

    [Fact]
    public async Task CreateGame_StartsEmpty_WithXToPlay()
    {
        var state = await CreateAsync();

        Assert.Equal(GameStatus.InProgress, state.Status);
        Assert.Equal(Player.X, state.CurrentPlayer);
        Assert.Equal(GameMode.TwoPlayer, state.Mode);
        Assert.Empty(state.Moves);
        Assert.False(state.CanUndo);
        Assert.All(state.Board, cell => Assert.Null(cell));
    }

    [Fact]
    public async Task SubmitMove_PlacesTheMark_AndPassesTheTurn()
    {
        var game = await CreateAsync();

        var state = await MoveAsync(game.Id, Player.X, 4);

        Assert.Equal(Player.X, state.Board[4]);
        Assert.Equal(Player.O, state.CurrentPlayer);
        Assert.True(state.CanUndo);
    }

    [Fact]
    public async Task MoveHistory_RecordsNumberPlayerAndPosition()
    {
        var game = await CreateAsync();
        await MoveAsync(game.Id, Player.X, 0);
        var state = await MoveAsync(game.Id, Player.O, 4);

        Assert.Collection(
            state.Moves,
            first =>
            {
                Assert.Equal(1, first.MoveNumber);
                Assert.Equal(Player.X, first.Player);
                Assert.Equal("Row 1, Column 1", first.Position);
            },
            second =>
            {
                Assert.Equal(2, second.MoveNumber);
                Assert.Equal(Player.O, second.Player);
                Assert.Equal("Row 2, Column 2", second.Position);
            });
    }

    [Fact]
    public async Task SubmitMove_OnAnOccupiedCell_IsRejected()
    {
        var game = await CreateAsync();
        await MoveAsync(game.Id, Player.X, 4);

        var error = await Assert.ThrowsAsync<MoveRejectedException>(
            () => MoveAsync(game.Id, Player.O, 4));

        Assert.Equal(MoveRejectionReason.CellOccupied, error.Reason);
    }

    [Fact]
    public async Task SubmitMove_ByTheWrongPlayer_IsRejected_AndTheTurnIsUnchanged()
    {
        var game = await CreateAsync();
        await MoveAsync(game.Id, Player.X, 4);

        var error = await Assert.ThrowsAsync<MoveRejectedException>(
            () => MoveAsync(game.Id, Player.X, 0));

        Assert.Equal(MoveRejectionReason.WrongPlayer, error.Reason);

        var state = await _harness.RunAsync(s => s.GetGameAsync(game.Id));
        Assert.Equal(Player.O, state.CurrentPlayer);
        Assert.Single(state.Moves);
    }

    [Fact]
    public async Task SubmitMove_OutsideTheBoard_IsRejected()
    {
        var game = await CreateAsync();

        var error = await Assert.ThrowsAsync<MoveRejectedException>(
            () => MoveAsync(game.Id, Player.X, 9));

        Assert.Equal(MoveRejectionReason.OutOfBoard, error.Reason);
    }

    [Fact]
    public async Task SubmitMove_AfterCompletion_IsRejected()
    {
        var game = await CreateAsync();
        await PlayXWinAsync(game.Id);

        var error = await Assert.ThrowsAsync<MoveRejectedException>(
            () => MoveAsync(game.Id, Player.O, 5));

        Assert.Equal(MoveRejectionReason.GameCompleted, error.Reason);
    }

    [Fact]
    public async Task SubmitMove_OnAnUnknownGame_IsRejected()
    {
        await Assert.ThrowsAsync<GameNotFoundException>(
            () => MoveAsync(Guid.NewGuid(), Player.X, 0));
    }

    [Fact]
    public async Task Win_ReportsTheWinnerAndHighlightCells_AndScoresOnce()
    {
        var game = await CreateAsync();

        var state = await PlayXWinAsync(game.Id);

        Assert.Equal(GameStatus.Won, state.Status);
        Assert.Equal(Player.X, state.Winner);
        Assert.Equal(new[] { 0, 1, 2 }, state.WinningCells);
        Assert.Equal(1, state.Scoreboard.XWins);

        // Reading the game again must not count the win a second time.
        var reread = await _harness.RunAsync(s => s.GetGameAsync(game.Id));
        Assert.Equal(1, reread.Scoreboard.XWins);
    }

    [Fact]
    public async Task Draw_IsDetected_AndCounted()
    {
        var game = await CreateAsync();

        var state = await PlayAsync(game.Id, 0, 1, 2, 4, 3, 5, 7, 6, 8);

        Assert.Equal(GameStatus.Draw, state.Status);
        Assert.Null(state.Winner);
        Assert.Equal(1, state.Scoreboard.Draws);
        Assert.Equal(0, state.Scoreboard.XWins);
    }

    [Fact]
    public async Task Undo_InTwoPlayerMode_RemovesOnlyTheLastMove()
    {
        var game = await CreateAsync();
        await PlayAsync(game.Id, 0, 4);

        var state = await _harness.RunAsync(s => s.UndoAsync(game.Id));

        Assert.Single(state.Moves);
        Assert.Equal(Player.X, state.Board[0]);
        Assert.Null(state.Board[4]);
        Assert.Equal(Player.O, state.CurrentPlayer);
    }

    [Fact]
    public async Task Undo_WithNoMoves_IsRejected()
    {
        var game = await CreateAsync();

        await Assert.ThrowsAsync<UndoNotAvailableException>(
            () => _harness.RunAsync(s => s.UndoAsync(game.Id)));
    }

    [Fact]
    public async Task Undo_AfterCompletion_ReversesTheResultOnTheScoreboard()
    {
        var game = await CreateAsync();
        var won = await PlayXWinAsync(game.Id);
        Assert.Equal(1, won.Scoreboard.XWins);

        var state = await _harness.RunAsync(s => s.UndoAsync(game.Id));

        Assert.Equal(GameStatus.InProgress, state.Status);
        Assert.Null(state.Winner);
        Assert.Empty(state.WinningCells);
        Assert.Equal(0, state.Scoreboard.XWins);
        Assert.Equal(Player.X, state.CurrentPlayer);
    }

    [Fact]
    public async Task Undo_ThenReplayingTheWin_CountsItOnceAgain()
    {
        var game = await CreateAsync();
        await PlayXWinAsync(game.Id);
        await _harness.RunAsync(s => s.UndoAsync(game.Id));

        var state = await MoveAsync(game.Id, Player.X, 2);

        Assert.Equal(GameStatus.Won, state.Status);
        Assert.Equal(1, state.Scoreboard.XWins);
    }

    [Fact]
    public async Task ComputerMode_RepliesAutomaticallyAfterTheHumanMove()
    {
        var game = await CreateAsync(GameMode.Computer);

        var state = await MoveAsync(game.Id, Player.X, 0);

        Assert.Equal(2, state.Moves.Count);
        Assert.Equal(Player.O, state.Moves[1].Player);
        Assert.Equal(ComputerPlayer.Center, state.Moves[1].CellIndex);
        Assert.Equal(Player.X, state.CurrentPlayer);
    }

    [Fact]
    public async Task ComputerMode_RejectsAClientPlayingO()
    {
        var game = await CreateAsync(GameMode.Computer);
        await MoveAsync(game.Id, Player.X, 0);

        var error = await Assert.ThrowsAsync<MoveRejectedException>(
            () => MoveAsync(game.Id, Player.O, 8));

        Assert.Equal(MoveRejectionReason.NotHumanControlled, error.Reason);
    }

    [Fact]
    public async Task ComputerMode_DoesNotMoveOnceTheGameIsComplete()
    {
        var game = await CreateAsync(GameMode.Computer);

        // X: 0 -> O takes centre. X: 1 -> O blocks at 2. X: 3 -> O completes 2,4,6.
        await MoveAsync(game.Id, Player.X, 0);
        await MoveAsync(game.Id, Player.X, 1);
        var state = await MoveAsync(game.Id, Player.X, 3);

        Assert.Equal(GameStatus.Won, state.Status);
        Assert.Equal(Player.O, state.Winner);
        Assert.Equal(6, state.Moves.Count);
        Assert.Equal(new[] { 2, 4, 6 }, state.WinningCells);
        Assert.Equal(1, state.Scoreboard.OWins);
    }

    [Fact]
    public async Task Undo_InComputerMode_RemovesTheMovePair_AndReturnsTheTurnToX()
    {
        var game = await CreateAsync(GameMode.Computer);
        await MoveAsync(game.Id, Player.X, 0);
        await MoveAsync(game.Id, Player.X, 1);

        var state = await _harness.RunAsync(s => s.UndoAsync(game.Id));

        // X:0 and O:4 remain; X:1 and the computer's block both came off.
        Assert.Equal(2, state.Moves.Count);
        Assert.Equal(Player.X, state.CurrentPlayer);
        Assert.Null(state.Board[1]);
        Assert.Equal(Player.X, state.Board[0]);
        Assert.Equal(Player.O, state.Board[ComputerPlayer.Center]);
    }

    [Fact]
    public async Task Undo_InComputerMode_DoesNotTriggerAnotherComputerMove()
    {
        var game = await CreateAsync(GameMode.Computer);
        await MoveAsync(game.Id, Player.X, 0);

        var state = await _harness.RunAsync(s => s.UndoAsync(game.Id));

        Assert.Empty(state.Moves);
        Assert.Equal(Player.X, state.CurrentPlayer);
    }

    [Fact]
    public async Task ResetGame_ClearsTheBoardAndHistory_ButKeepsTheScoreboard()
    {
        var game = await CreateAsync();
        await PlayXWinAsync(game.Id);

        var state = await _harness.RunAsync(s => s.ResetGameAsync(game.Id));

        Assert.Empty(state.Moves);
        Assert.All(state.Board, cell => Assert.Null(cell));
        Assert.Equal(GameStatus.InProgress, state.Status);
        Assert.Null(state.Winner);
        Assert.Equal(Player.X, state.CurrentPlayer);
        Assert.False(state.CanUndo);
        Assert.Equal(1, state.Scoreboard.XWins);
    }

    [Fact]
    public async Task ResetGame_CanSwitchTheMode()
    {
        var game = await CreateAsync();
        await MoveAsync(game.Id, Player.X, 0);

        var state = await _harness.RunAsync(s => s.ResetGameAsync(game.Id, GameMode.Computer));

        Assert.Equal(GameMode.Computer, state.Mode);
        Assert.Empty(state.Moves);
    }

    [Fact]
    public async Task ResetGame_ThenWinningAgain_AddsASecondWin()
    {
        var game = await CreateAsync();
        await PlayXWinAsync(game.Id);
        await _harness.RunAsync(s => s.ResetGameAsync(game.Id));

        var state = await PlayXWinAsync(game.Id);

        Assert.Equal(2, state.Scoreboard.XWins);
    }

    [Fact]
    public async Task ResetScoreboard_ZeroesTheTallies()
    {
        var game = await CreateAsync();
        await PlayXWinAsync(game.Id);

        var scoreboard = await _harness.RunAsync(s => s.ResetScoreboardAsync());

        Assert.Equal(new ScoreboardResponse(0, 0, 0), scoreboard);
    }

    [Fact]
    public async Task Scoreboard_IsSharedAcrossSessions()
    {
        var first = await CreateAsync();
        await PlayXWinAsync(first.Id);

        var second = await CreateAsync();

        Assert.Equal(1, second.Scoreboard.XWins);
    }

    [Fact]
    public async Task UndoDepth_IsReportedForTheClient()
    {
        var twoPlayer = await CreateAsync();
        await PlayAsync(twoPlayer.Id, 0, 4);
        var twoPlayerState = await _harness.RunAsync(s => s.GetGameAsync(twoPlayer.Id));
        Assert.Equal(1, twoPlayerState.UndoDepth);

        var computer = await CreateAsync(GameMode.Computer);
        await MoveAsync(computer.Id, Player.X, 0);
        var computerState = await _harness.RunAsync(s => s.GetGameAsync(computer.Id));
        Assert.Equal(2, computerState.UndoDepth);
    }
}
