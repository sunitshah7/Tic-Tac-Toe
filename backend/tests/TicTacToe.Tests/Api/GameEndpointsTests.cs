using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TicTacToe.Api.Contracts;
using TicTacToe.Domain;

namespace TicTacToe.Tests.Api;

/// <summary>
/// The REST contract as a client sees it: status codes, the problem-details shape used for
/// rejections, and the fact that every game action answers with the full game state.
/// </summary>
public sealed class GameEndpointsTests : IDisposable
{
    private readonly TicTacToeApiFactory _factory = new();
    private readonly HttpClient _client;

    public GameEndpointsTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static readonly JsonSerializerOptions Json = TicTacToeApiFactory.Json;

    private async Task<GameStateResponse> CreateGameAsync(GameMode mode = GameMode.TwoPlayer)
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { mode = mode.ToString() }, Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<GameStateResponse>(Json))!;
    }

    private Task<HttpResponseMessage> PostMoveAsync(Guid id, object body) =>
        _client.PostAsJsonAsync($"/api/games/{id}/moves", body, Json);

    [Fact]
    public async Task CreateGame_Returns201_WithALocationHeader()
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" }, Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var state = await response.Content.ReadFromJsonAsync<GameStateResponse>(Json);
        Assert.NotNull(state);
        Assert.NotEqual(Guid.Empty, state!.Id);
        Assert.Equal(Player.X, state.CurrentPlayer);
    }

    [Fact]
    public async Task GetGame_ReturnsTheCurrentState()
    {
        var game = await CreateGameAsync();

        var state = await _client.GetFromJsonAsync<GameStateResponse>($"/api/games/{game.Id}", Json);

        Assert.NotNull(state);
        Assert.Equal(game.Id, state!.Id);
        Assert.Equal(GameStatus.InProgress, state.Status);
    }

    [Fact]
    public async Task GetGame_ForAnUnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/games/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("GameNotFound", await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task SubmitMove_AcceptsRowAndColumn()
    {
        var game = await CreateGameAsync();

        var response = await PostMoveAsync(game.Id, new { player = "X", row = 1, column = 1 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var state = await response.Content.ReadFromJsonAsync<GameStateResponse>(Json);
        Assert.Equal(Player.X, state!.Board[4]);
        Assert.Equal("Row 2, Column 2", state.Moves[0].Position);
    }

    [Fact]
    public async Task SubmitMove_AcceptsACellIndex()
    {
        var game = await CreateGameAsync();

        var response = await PostMoveAsync(game.Id, new { player = "X", cellIndex = 8 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var state = await response.Content.ReadFromJsonAsync<GameStateResponse>(Json);
        Assert.Equal(Player.X, state!.Board[8]);
    }

    [Fact]
    public async Task SubmitMove_OnAnOccupiedCell_Returns409()
    {
        var game = await CreateGameAsync();
        await PostMoveAsync(game.Id, new { player = "X", cellIndex = 0 });

        var response = await PostMoveAsync(game.Id, new { player = "O", cellIndex = 0 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("CellOccupied", await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task SubmitMove_ByTheWrongPlayer_Returns409()
    {
        var game = await CreateGameAsync();
        await PostMoveAsync(game.Id, new { player = "X", cellIndex = 0 });

        var response = await PostMoveAsync(game.Id, new { player = "X", cellIndex = 1 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("WrongPlayer", await ReadErrorCodeAsync(response));
    }

    [Theory]
    [InlineData(3, 0)]
    [InlineData(0, 3)]
    [InlineData(-1, 0)]
    public async Task SubmitMove_OffTheBoard_Returns400(int row, int column)
    {
        var game = await CreateGameAsync();

        var response = await PostMoveAsync(game.Id, new { player = "X", row, column });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("OutOfBoard", await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task SubmitMove_WithoutACell_Returns400()
    {
        var game = await CreateGameAsync();

        var response = await PostMoveAsync(game.Id, new { player = "X" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidMoveRequest", await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task SubmitMove_AfterCompletion_Returns409()
    {
        var game = await CreateGameAsync();
        foreach (var (player, cell) in new[] { ("X", 0), ("O", 3), ("X", 1), ("O", 4), ("X", 2) })
        {
            var move = await PostMoveAsync(game.Id, new { player, cellIndex = cell });
            Assert.Equal(HttpStatusCode.OK, move.StatusCode);
        }

        var response = await PostMoveAsync(game.Id, new { player = "O", cellIndex = 5 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("GameCompleted", await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Undo_WithNoMoves_Returns409()
    {
        var game = await CreateGameAsync();

        var response = await _client.PostAsync($"/api/games/{game.Id}/undo", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("NothingToUndo", await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Undo_RestoresThePreviousState()
    {
        var game = await CreateGameAsync();
        await PostMoveAsync(game.Id, new { player = "X", cellIndex = 0 });
        await PostMoveAsync(game.Id, new { player = "O", cellIndex = 4 });

        var response = await _client.PostAsync($"/api/games/{game.Id}/undo", content: null);
        var state = await response.Content.ReadFromJsonAsync<GameStateResponse>(Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(state!.Moves);
        Assert.Equal(Player.O, state.CurrentPlayer);
    }

    [Fact]
    public async Task ResetGame_ClearsTheBoard()
    {
        var game = await CreateGameAsync();
        await PostMoveAsync(game.Id, new { player = "X", cellIndex = 0 });

        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/reset", new { mode = "Computer" }, Json);
        var state = await response.Content.ReadFromJsonAsync<GameStateResponse>(Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(state!.Moves);
        Assert.Equal(GameMode.Computer, state.Mode);
    }

    [Fact]
    public async Task ComputerMode_AnswersInTheSameResponse()
    {
        var game = await CreateGameAsync(GameMode.Computer);

        var response = await PostMoveAsync(game.Id, new { player = "X", cellIndex = 0 });
        var state = await response.Content.ReadFromJsonAsync<GameStateResponse>(Json);

        Assert.Equal(2, state!.Moves.Count);
        Assert.Equal(Player.O, state.Moves[1].Player);
        Assert.Equal(Player.X, state.CurrentPlayer);
    }

    [Fact]
    public async Task Scoreboard_IsReadableAndResettable()
    {
        var game = await CreateGameAsync();
        foreach (var (player, cell) in new[] { ("X", 0), ("O", 3), ("X", 1), ("O", 4), ("X", 2) })
        {
            await PostMoveAsync(game.Id, new { player, cellIndex = cell });
        }

        var scoreboard = await _client.GetFromJsonAsync<ScoreboardResponse>("/api/scoreboard", Json);
        Assert.Equal(1, scoreboard!.XWins);

        var reset = await _client.PostAsync("/api/scoreboard/reset", content: null);
        var cleared = await reset.Content.ReadFromJsonAsync<ScoreboardResponse>(Json);

        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.Equal(new ScoreboardResponse(0, 0, 0), cleared);
    }

    /// <summary>Pulls the <c>errorCode</c> extension out of a problem-details body.</summary>
    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
    }
}
