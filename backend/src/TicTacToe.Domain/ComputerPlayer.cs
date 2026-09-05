namespace TicTacToe.Domain;

/// <summary>
/// The "basic computer opponent" from the specification: a fixed priority ladder rather
/// than a search. It is deliberately deterministic — among equally ranked candidates it
/// always takes the lowest cell index — so that its behaviour is unit testable and a
/// reviewer can predict any given reply from the board alone.
/// </summary>
public static class ComputerPlayer
{
    /// <summary>Middle cell.</summary>
    public const int Center = 4;

    /// <summary>Corner cells, in the order they are preferred.</summary>
    public static readonly IReadOnlyList<int> Corners = new[] { 0, 2, 6, 8 };

    /// <summary>
    /// Picks a cell for <paramref name="player"/>, or null when the board is full.
    /// Priority: take the win, else block the opponent's win, else centre, else a corner,
    /// else the first free cell.
    /// </summary>
    public static int? SelectMove(IReadOnlyList<Player?> board, Player player)
    {
        ArgumentNullException.ThrowIfNull(board);

        // 1. Win now if the line is there.
        var winning = FindCompletingCell(board, player);
        if (winning is not null)
        {
            return winning;
        }

        // 2. Otherwise deny the opponent the same opportunity.
        var blocking = FindCompletingCell(board, GameEngine.Opponent(player));
        if (blocking is not null)
        {
            return blocking;
        }

        // 3. Centre, 4. corner, 5. anything.
        if (board[Center] is null)
        {
            return Center;
        }

        foreach (var corner in Corners)
        {
            if (board[corner] is null)
            {
                return corner;
            }
        }

        for (var cell = 0; cell < GameEngine.CellCount; cell++)
        {
            if (board[cell] is null)
            {
                return cell;
            }
        }

        return null;
    }

    /// <summary>
    /// The empty cell that would complete a line for <paramref name="player"/>, if one exists.
    /// </summary>
    public static int? FindCompletingCell(IReadOnlyList<Player?> board, Player player)
    {
        ArgumentNullException.ThrowIfNull(board);

        foreach (var line in GameEngine.WinningLines)
        {
            var owned = 0;
            int? empty = null;

            foreach (var cell in line)
            {
                if (board[cell] == player)
                {
                    owned++;
                }
                else if (board[cell] is null)
                {
                    empty = cell;
                }
                else
                {
                    // Opponent mark on this line: it can never be completed.
                    owned = -1;
                    break;
                }
            }

            if (owned == 2 && empty is not null)
            {
                return empty;
            }
        }

        return null;
    }
}
