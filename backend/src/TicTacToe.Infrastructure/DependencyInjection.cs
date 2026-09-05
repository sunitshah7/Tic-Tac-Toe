using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicTacToe.Domain.Abstractions;
using TicTacToe.Infrastructure.Persistence;

namespace TicTacToe.Infrastructure;

/// <summary>Composition root for the persistence layer.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the SQLite context and the store implementations behind their domain ports.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<GameDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IGameStore, GameStore>();
        services.AddScoped<IScoreboardStore, ScoreboardStore>();

        return services;
    }
}
