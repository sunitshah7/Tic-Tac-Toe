using System.Globalization;
using TicTacToe.Domain;

namespace TicTacToe.Api.Contracts;

/// <summary>Projects domain state onto the wire contract.</summary>
public static class ResponseMapper
{
    /// <summary>Builds the full game-state response for a session and the current scoreboard.</summary>
    public static GameStateResponse ToResponse(Game game, GameSnapshot snapshot, Scoreboard scoreboard)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(scoreboard);

        return new GameStateResponse
        {
            Id = game.Id,
            Mode = snapshot.Mode,
            Board = snapshot.Board,
            CurrentPlayer = snapshot.CurrentPlayer,
            Status = snapshot.Status,
            Winner = snapshot.Winner,
            WinningCells = snapshot.WinningCells,
            Moves = snapshot.Moves.Select(ToResponse).ToList(),
            CanUndo = snapshot.Moves.Count > 0,
            UndoDepth = GameEngine.UndoDepth(snapshot.Mode, snapshot.Moves),
            Scoreboard = ToResponse(scoreboard)
        };
    }

    /// <summary>Projects a single move, including its human-readable position.</summary>
    public static MoveResponse ToResponse(PlacedMove move)
    {
        ArgumentNullException.ThrowIfNull(move);

        return new MoveResponse(
            move.MoveNumber,
            move.Player,
            move.Row,
            move.Column,
            move.CellIndex,
            string.Format(
                CultureInfo.InvariantCulture,
                "Row {0}, Column {1}",
                move.Row + 1,
                move.Column + 1));
    }

    /// <summary>Projects the scoreboard.</summary>
    public static ScoreboardResponse ToResponse(Scoreboard scoreboard)
    {
        ArgumentNullException.ThrowIfNull(scoreboard);
        return new ScoreboardResponse(scoreboard.XWins, scoreboard.OWins, scoreboard.Draws);
    }
}
