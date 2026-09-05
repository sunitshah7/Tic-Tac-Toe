using Microsoft.EntityFrameworkCore;
using TicTacToe.Domain;
using TicTacToe.Domain.Abstractions;

namespace TicTacToe.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IScoreboardStore"/> over the single seeded
/// scoreboard row.
/// </summary>
public sealed class ScoreboardStore : IScoreboardStore
{
    private readonly GameDbContext _context;

    public ScoreboardStore(GameDbContext context) => _context = context;

    public async Task<Scoreboard> GetAsync(CancellationToken cancellationToken = default)
    {
        var entity = await _context.Scoreboards
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == ScoreboardEntity.SingletonId, cancellationToken)
            .ConfigureAwait(false);

        return entity is null
            ? Scoreboard.Empty
            : new Scoreboard(entity.XWins, entity.OWins, entity.Draws);
    }

    public async Task SaveAsync(Scoreboard scoreboard, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scoreboard);

        var entity = await _context.Scoreboards
            .FirstOrDefaultAsync(s => s.Id == ScoreboardEntity.SingletonId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            entity = new ScoreboardEntity { Id = ScoreboardEntity.SingletonId };
            _context.Scoreboards.Add(entity);
        }

        entity.XWins = scoreboard.XWins;
        entity.OWins = scoreboard.OWins;
        entity.Draws = scoreboard.Draws;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
