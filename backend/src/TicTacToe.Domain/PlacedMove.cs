namespace TicTacToe.Domain;

/// <summary>
/// A single mark placed on the board. The ordered sequence of these is the
/// authoritative state of a game: board, turn, status, winner and winning cells
/// are all derived from it, which is what makes undo a simple truncation.
/// </summary>
/// <param name="MoveNumber">1-based position in the game's move sequence.</param>
/// <param name="Player">The mark that was placed.</param>
/// <param name="CellIndex">Flat board index, 0..8, row-major.</param>
public sealed record PlacedMove(int MoveNumber, Player Player, int CellIndex)
{
    /// <summary>0-based row.</summary>
    public int Row => CellIndex / 3;

    /// <summary>0-based column.</summary>
    public int Column => CellIndex % 3;
}
