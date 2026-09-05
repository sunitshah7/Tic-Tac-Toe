namespace TicTacToe.Domain;

/// <summary>The three outcomes a finished game can contribute to the scoreboard.</summary>
public enum GameResult
{
    XWin = 0,
    OWin = 1,
    Draw = 2
}

/// <summary>
/// Session-level tallies. Immutable: applying or reverting a result yields a new value,
/// which keeps the "update exactly once per completed game" rule easy to reason about —
/// the caller decides when to apply, the type only ever does arithmetic.
/// </summary>
public sealed record Scoreboard(int XWins, int OWins, int Draws)
{
    /// <summary>A fresh, all-zero scoreboard.</summary>
    public static Scoreboard Empty => new(0, 0, 0);

    /// <summary>Adds a completed game's result.</summary>
    public Scoreboard Apply(GameResult result) => result switch
    {
        GameResult.XWin => this with { XWins = XWins + 1 },
        GameResult.OWin => this with { OWins = OWins + 1 },
        GameResult.Draw => this with { Draws = Draws + 1 },
        _ => this
    };

    /// <summary>
    /// Removes a previously applied result, used when an Undo reverses a finished game.
    /// Clamped at zero so a bookkeeping slip can never produce a negative tally.
    /// </summary>
    public Scoreboard Revert(GameResult result) => result switch
    {
        GameResult.XWin => this with { XWins = Math.Max(0, XWins - 1) },
        GameResult.OWin => this with { OWins = Math.Max(0, OWins - 1) },
        GameResult.Draw => this with { Draws = Math.Max(0, Draws - 1) },
        _ => this
    };
}
