namespace TicTacToe.Domain.Abstractions;

/// <summary>Persistence port for the single session-level scoreboard.</summary>
public interface IScoreboardStore
{
    /// <summary>Reads the current tallies, returning <see cref="Scoreboard.Empty"/> if none stored yet.</summary>
    Task<Scoreboard> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Overwrites the stored tallies.</summary>
    Task SaveAsync(Scoreboard scoreboard, CancellationToken cancellationToken = default);
}
