using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicTacToe.Api.Services;
using TicTacToe.Infrastructure;
using TicTacToe.Infrastructure.Persistence;

namespace TicTacToe.Tests.Services;

/// <summary>
/// Hosts <see cref="GameService"/> over a private in-memory SQLite database, wired through
/// the same DI registrations the application uses. Each operation runs in its own scope, so
/// the tests exercise the real per-request DbContext lifetime rather than a shared context
/// that would mask tracking bugs.
/// </summary>
internal sealed class GameServiceHarness : IDisposable
{
    private readonly SqliteConnection _keepAlive;
    private readonly ServiceProvider _provider;

    public GameServiceHarness()
    {
        // A shared-cache in-memory database lives only as long as one connection to it is
        // open, so the harness holds one for its lifetime.
        var connectionString = $"Data Source=file:tictactoe-{Guid.NewGuid():N}?mode=memory&cache=shared";
        _keepAlive = new SqliteConnection(connectionString);
        _keepAlive.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(connectionString);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<GameSessionLocks>();
        services.AddScoped<GameService>();
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<GameDbContext>().Database.EnsureCreated();
    }

    /// <summary>Runs one service operation in a fresh scope and returns its result.</summary>
    public async Task<T> RunAsync<T>(Func<GameService, Task<T>> operation)
    {
        using var scope = _provider.CreateScope();
        return await operation(scope.ServiceProvider.GetRequiredService<GameService>());
    }

    public void Dispose()
    {
        _provider.Dispose();
        _keepAlive.Dispose();
    }
}
