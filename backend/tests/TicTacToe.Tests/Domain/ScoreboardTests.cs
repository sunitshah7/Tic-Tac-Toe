using TicTacToe.Domain;

namespace TicTacToe.Tests.Domain;

/// <summary>Arithmetic of the session scoreboard, including the Option B reversal path.</summary>
public sealed class ScoreboardTests
{
    [Fact]
    public void Empty_StartsAtZero()
    {
        var scoreboard = Scoreboard.Empty;

        Assert.Equal(0, scoreboard.XWins);
        Assert.Equal(0, scoreboard.OWins);
        Assert.Equal(0, scoreboard.Draws);
    }

    [Theory]
    [InlineData(GameResult.XWin, 1, 0, 0)]
    [InlineData(GameResult.OWin, 0, 1, 0)]
    [InlineData(GameResult.Draw, 0, 0, 1)]
    public void Apply_IncrementsTheMatchingTally(GameResult result, int x, int o, int draws)
    {
        var scoreboard = Scoreboard.Empty.Apply(result);

        Assert.Equal(x, scoreboard.XWins);
        Assert.Equal(o, scoreboard.OWins);
        Assert.Equal(draws, scoreboard.Draws);
    }

    [Fact]
    public void Revert_UndoesAnAppliedResult()
    {
        var scoreboard = Scoreboard.Empty
            .Apply(GameResult.XWin)
            .Apply(GameResult.XWin)
            .Revert(GameResult.XWin);

        Assert.Equal(1, scoreboard.XWins);
    }

    [Fact]
    public void Revert_NeverGoesNegative()
    {
        var scoreboard = Scoreboard.Empty.Revert(GameResult.Draw);

        Assert.Equal(0, scoreboard.Draws);
    }

    [Fact]
    public void Apply_LeavesTheOtherTalliesAlone()
    {
        var scoreboard = Scoreboard.Empty
            .Apply(GameResult.XWin)
            .Apply(GameResult.Draw)
            .Apply(GameResult.OWin);

        Assert.Equal(new Scoreboard(1, 1, 1), scoreboard);
    }
}
