using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace TicTacToe.Tests.Api;

/// <summary>
/// Hosts the real API in process, pointed at a private in-memory SQLite database so the
/// tests exercise the actual routing, model binding, serialisation and problem-details
/// pipeline without touching the developer's tictactoe.db file.
/// </summary>
internal sealed class TicTacToeApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _keepAlive;
    private readonly string _connectionString;

    public TicTacToeApiFactory()
    {
        _connectionString = $"Data Source=file:tictactoe-api-{Guid.NewGuid():N}?mode=memory&cache=shared";
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    /// <summary>Matches the serialisation the API itself uses, so responses round-trip.</summary>
    public static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:GameDatabase", _connectionString);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _keepAlive.Dispose();
        }
    }
}
