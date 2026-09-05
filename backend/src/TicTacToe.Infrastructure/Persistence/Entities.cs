using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicTacToe.Infrastructure.Persistence;

/// <summary>
/// Relational shape of a game session. Kept separate from the domain <c>Game</c> so that
/// storage concerns (surrogate keys, foreign keys, enum-to-int conversion) never leak into
/// the rules. Mapping lives in <see cref="GameStore"/>.
/// </summary>
public sealed class GameEntity
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>Persisted <c>GameMode</c> as its integer value.</summary>
    public int Mode { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Persisted <c>GameResult</c> already counted on the scoreboard, or null.</summary>
    public int? RecordedResult { get; set; }

    public List<MoveEntity> Moves { get; set; } = new();
}

/// <summary>Relational shape of a single placed mark.</summary>
public sealed class MoveEntity
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Game))]
    public Guid GameId { get; set; }

    public GameEntity? Game { get; set; }

    public int MoveNumber { get; set; }

    /// <summary>Persisted <c>Player</c> as its integer value.</summary>
    public int Player { get; set; }

    public int CellIndex { get; set; }
}

/// <summary>
/// Relational shape of the scoreboard. Exactly one row exists, at
/// <see cref="SingletonId"/>, because the specification calls for one session-level tally.
/// </summary>
public sealed class ScoreboardEntity
{
    /// <summary>The id of the one and only scoreboard row.</summary>
    public const int SingletonId = 1;

    [Key]
    public int Id { get; set; }

    public int XWins { get; set; }

    public int OWins { get; set; }

    public int Draws { get; set; }
}
