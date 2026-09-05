using TicTacToe.Domain;

namespace TicTacToe.Tests.Domain;

/// <summary>
/// The computer opponent's priority ladder: win, block, centre, corner, anything. Each test
/// sets up a board where exactly one rung applies, so a regression points at the rung that broke.
/// </summary>
public sealed class ComputerPlayerTests
{
    /// <summary>Builds a board from a nine-character string; '.' is empty.</summary>
    private static Player?[] Board(string layout)
    {
        Assert.Equal(GameEngine.CellCount, layout.Length);

        return layout.Select<char, Player?>(c => c switch
        {
            'X' => Player.X,
            'O' => Player.O,
            _ => null
        }).ToArray();
    }

    [Fact]
    public void TakesTheWin_WhenOneIsAvailable()
    {
        //  O O .        O completes the top row at cell 2.
        //  X X .
        //  . . .
        var move = ComputerPlayer.SelectMove(Board("OO.XX...."), Player.O);

        Assert.Equal(2, move);
    }

    [Fact]
    public void PrefersItsOwnWin_OverBlocking()
    {
        //  O O .        O can win at 2; X could win at 5. Winning comes first.
        //  X X .
        //  . . .
        var move = ComputerPlayer.SelectMove(Board("OO.XX...."), Player.O);

        Assert.Equal(2, move);
        Assert.Equal(5, ComputerPlayer.FindCompletingCell(Board("OO.XX...."), Player.X));
    }

    [Fact]
    public void BlocksTheOpponent_WhenItCannotWinItself()
    {
        //  X X .        X threatens the top row; O has no line of its own.
        //  . O .
        //  . . .
        var move = ComputerPlayer.SelectMove(Board("XX..O...."), Player.O);

        Assert.Equal(2, move);
    }

    [Fact]
    public void TakesTheCentre_WhenNothingIsUrgent()
    {
        //  X . .
        //  . . .
        //  . . .
        var move = ComputerPlayer.SelectMove(Board("X........"), Player.O);

        Assert.Equal(ComputerPlayer.Center, move);
    }

    [Fact]
    public void TakesACorner_WhenTheCentreIsGone()
    {
        //  . . .        Centre taken by X, no threats yet: first free corner is 0.
        //  . X .
        //  . . .
        var move = ComputerPlayer.SelectMove(Board("....X...."), Player.O);

        Assert.Equal(0, move);
    }

    [Fact]
    public void FallsBackToTheFirstFreeCell_WhenNoHigherRungApplies()
    {
        //  X X O        Centre and every corner are taken, neither side has a line with a
        //  O O X        gap in it, and cell 7 is the only square left. The ladder falls all
        //  X . O        the way through to "take any available cell".
        var move = ComputerPlayer.SelectMove(Board("XXOOOXX.O"), Player.O);

        Assert.Equal(7, move);
    }

    [Fact]
    public void ReturnsNull_WhenTheBoardIsFull()
    {
        var move = ComputerPlayer.SelectMove(Board("XOXXOOOXX"), Player.O);

        Assert.Null(move);
    }

    [Fact]
    public void IgnoresLines_TheOpponentAlreadyBlocks()
    {
        //  O O X        The top row is dead; O must not "complete" it.
        //  . . .
        //  . . .
        Assert.Null(ComputerPlayer.FindCompletingCell(Board("OOX......"), Player.O));
    }

    [Fact]
    public void ChoosesDeterministically_AmongEqualCandidates()
    {
        var board = Board("....X....");

        // Repeated calls on the same board always yield the same cell, which is what makes
        // the opponent's behaviour reproducible in tests and in a panel demo.
        Assert.Equal(
            ComputerPlayer.SelectMove(board, Player.O),
            ComputerPlayer.SelectMove(board, Player.O));
    }
}
