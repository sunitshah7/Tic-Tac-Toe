using System.Collections.Concurrent;

namespace TicTacToe.Api.Services;

/// <summary>
/// Serialises mutating operations per game session. Every mutation is a read-modify-write
/// over the move list, so two requests arriving together for the same game could otherwise
/// interleave and lose a move. Registered as a singleton; the lock set is per process,
/// which is sufficient for the single-instance local deployment this exercise targets.
/// </summary>
public sealed class GameSessionLocks : IDisposable
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    /// <summary>Acquires the lock for a session; dispose the result to release it.</summary>
    public async Task<IDisposable> AcquireAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var gate = _locks.GetOrAdd(gameId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Release(gate);
    }

    public void Dispose()
    {
        foreach (var gate in _locks.Values)
        {
            gate.Dispose();
        }

        _locks.Clear();
    }

    private sealed class Release : IDisposable
    {
        private readonly SemaphoreSlim _gate;
        private bool _released;

        public Release(SemaphoreSlim gate) => _gate = gate;

        public void Dispose()
        {
            if (_released)
            {
                return;
            }

            _released = true;
            _gate.Release();
        }
    }
}
