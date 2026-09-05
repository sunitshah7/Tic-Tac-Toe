using System.ComponentModel.DataAnnotations;
using TicTacToe.Domain;

namespace TicTacToe.Api.Contracts;

/// <summary>Body of <c>POST /api/games</c>.</summary>
public sealed record CreateGameRequest
{
    /// <summary>Which mode the new session is played in. Defaults to Two Player.</summary>
    public GameMode Mode { get; init; } = GameMode.TwoPlayer;
}

/// <summary>
/// Body of <c>POST /api/games/{id}/moves</c>. The target cell may be given either as a
/// flat <see cref="CellIndex"/> (0..8, row-major) or as <see cref="Row"/> plus
/// <see cref="Column"/> (0..2); the specification allows either, so both are accepted.
/// </summary>
public sealed record SubmitMoveRequest
{
    /// <summary>The mark being placed. Required, so that a move by the wrong player is rejected rather than assumed.</summary>
    [Required]
    public Player? Player { get; init; }

    /// <summary>0-based row. Used with <see cref="Column"/> when <see cref="CellIndex"/> is absent.</summary>
    public int? Row { get; init; }

    /// <summary>0-based column. Used with <see cref="Row"/> when <see cref="CellIndex"/> is absent.</summary>
    public int? Column { get; init; }

    /// <summary>0-based flat cell index. Takes precedence over row/column when both are supplied.</summary>
    public int? CellIndex { get; init; }
}
