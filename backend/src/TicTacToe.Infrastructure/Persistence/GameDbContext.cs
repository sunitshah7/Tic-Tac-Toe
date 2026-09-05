using Microsoft.EntityFrameworkCore;

namespace TicTacToe.Infrastructure.Persistence;

/// <summary>SQLite-backed store for game sessions and the scoreboard.</summary>
public sealed class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options)
        : base(options)
    {
    }

    public DbSet<GameEntity> Games => Set<GameEntity>();

    public DbSet<MoveEntity> Moves => Set<MoveEntity>();

    public DbSet<ScoreboardEntity> Scoreboards => Set<ScoreboardEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<GameEntity>(game =>
        {
            game.HasMany(g => g.Moves)
                .WithOne(m => m.Game!)
                .HasForeignKey(m => m.GameId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MoveEntity>(move =>
        {
            // A game can never hold two marks at the same ordinal or in the same cell;
            // the database enforces what the engine already guarantees.
            move.HasIndex(m => new { m.GameId, m.MoveNumber }).IsUnique();
            move.HasIndex(m => new { m.GameId, m.CellIndex }).IsUnique();
        });

        // Seed the single scoreboard row so reads never have to special-case its absence.
        modelBuilder.Entity<ScoreboardEntity>()
            .HasData(new ScoreboardEntity
            {
                Id = ScoreboardEntity.SingletonId,
                XWins = 0,
                OWins = 0,
                Draws = 0
            });
    }
}
