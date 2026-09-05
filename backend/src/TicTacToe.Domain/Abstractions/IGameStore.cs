namespace TicTacToe.Domain.Abstractions;

/// <summary>
/// Persistence port for game sessions. Declared in the domain and implemented in
/// Infrastructure so the rules never depend on Entity Framework; swapping SQLite for
/// another store is a single class.
/// </summary>
public interface IGameStore
{
    /// <summary>Returns the session, or null when the id is unknown.</summary>
    Task<Game?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists a newly created session.</summary>
    Task AddAsync(Game game, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing session, including move additions and removals.</summary>
    Task UpdateAsync(Game game, CancellationToken cancellationToken = default);
}
