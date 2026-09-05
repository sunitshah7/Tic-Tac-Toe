using Microsoft.EntityFrameworkCore;
using TicTacToe.Domain;
using TicTacToe.Domain.Abstractions;

namespace TicTacToe.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IGameStore"/>. Translates between the relational
/// entities and the domain aggregate, and reconciles the move rows on update so that both
/// appended moves and Undo truncations are persisted.
/// </summary>
public sealed class GameStore : IGameStore
{
    private readonly GameDbContext _context;

    public GameStore(GameDbContext context) => _context = context;

    public async Task<Game?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Games
            .Include(g => g.Moves)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddAsync(Game game, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(game);

        _context.Games.Add(new GameEntity
        {
            Id = game.Id,
            Mode = (int)game.Mode,
            CreatedAt = game.CreatedAt,
            RecordedResult = (int?)game.RecordedResult,
            Moves = game.Moves.Select(m => ToEntity(game.Id, m)).ToList()
        });

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Game game, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(game);

        var entity = await _context.Games
            .Include(g => g.Moves)
            .FirstOrDefaultAsync(g => g.Id == game.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Game {game.Id} no longer exists.");

        entity.Mode = (int)game.Mode;
        entity.RecordedResult = (int?)game.RecordedResult;

        // Drop rows the domain no longer has (Undo and Reset both shorten the list) ...
        var keptNumbers = game.Moves.Select(m => m.MoveNumber).ToHashSet();
        var removed = entity.Moves.Where(m => !keptNumbers.Contains(m.MoveNumber)).ToList();
        foreach (var move in removed)
        {
            entity.Moves.Remove(move);
            _context.Moves.Remove(move);
        }

        // ... then add the ones it has gained.
        var existingNumbers = entity.Moves.Select(m => m.MoveNumber).ToHashSet();
        foreach (var move in game.Moves.Where(m => !existingNumbers.Contains(m.MoveNumber)))
        {
            entity.Moves.Add(ToEntity(game.Id, move));
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static MoveEntity ToEntity(Guid gameId, PlacedMove move) => new()
    {
        GameId = gameId,
        MoveNumber = move.MoveNumber,
        Player = (int)move.Player,
        CellIndex = move.CellIndex
    };

    private static Game ToDomain(GameEntity entity) => new()
    {
        Id = entity.Id,
        Mode = (GameMode)entity.Mode,
        CreatedAt = entity.CreatedAt,
        RecordedResult = (GameResult?)entity.RecordedResult,
        Moves = entity.Moves
            .OrderBy(m => m.MoveNumber)
            .Select(m => new PlacedMove(m.MoveNumber, (Player)m.Player, m.CellIndex))
            .ToList()
    };
}
