using TicTacToe.Api.Contracts;
using TicTacToe.Domain;
using TicTacToe.Domain.Abstractions;

namespace TicTacToe.Api.Services;

/// <summary>
/// Application service that turns HTTP-level intentions into domain operations. It owns the
/// orchestration the rules deliberately do not: persistence, the automatic computer reply,
/// and keeping the scoreboard in step with game completion.
/// </summary>
public sealed class GameService
{
    private readonly IGameStore _games;
    private readonly IScoreboardStore _scoreboards;
    private readonly GameSessionLocks _locks;
    private readonly TimeProvider _clock;

    public GameService(
        IGameStore games,
        IScoreboardStore scoreboards,
        GameSessionLocks locks,
        TimeProvider clock)
    {
        _games = games;
        _scoreboards = scoreboards;
        _locks = locks;
        _clock = clock;
    }

    /// <summary>Creates a new session in the requested mode, with an empty board and X to play.</summary>
    public async Task<GameStateResponse> CreateGameAsync(GameMode mode, CancellationToken cancellationToken = default)
    {
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Mode = mode,
            CreatedAt = _clock.GetUtcNow()
        };

        await _games.AddAsync(game, cancellationToken).ConfigureAwait(false);
        return await BuildResponseAsync(game, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads the current state of a session.</summary>
    public async Task<GameStateResponse> GetGameAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await LoadAsync(id, cancellationToken).ConfigureAwait(false);
        return await BuildResponseAsync(game, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates and applies a human move, then, in computer mode and only while the game is
    /// still running, plays the engine reply in the same request so the client always
    /// receives a state it can act on.
    /// </summary>
    public async Task<GameStateResponse> SubmitMoveAsync(
        Guid id,
        Player player,
        int cellIndex,
        CancellationToken cancellationToken = default)
    {
        using var handle = await _locks.AcquireAsync(id, cancellationToken).ConfigureAwait(false);

        var game = await LoadAsync(id, cancellationToken).ConfigureAwait(false);
        var snapshot = game.Snapshot();

        var rejection = GameEngine.ValidateMove(snapshot, player, cellIndex);
        if (rejection != MoveRejectionReason.None)
        {
            throw new MoveRejectedException(rejection);
        }

        game.Moves.Add(GameEngine.NextMove(game.Moves, player, cellIndex));
        snapshot = game.Snapshot();

        if (snapshot.Mode == GameMode.Computer && !snapshot.IsComplete)
        {
            var reply = ComputerPlayer.SelectMove(snapshot.Board, Player.O);
            if (reply is not null)
            {
                game.Moves.Add(GameEngine.NextMove(game.Moves, Player.O, reply.Value));
                snapshot = game.Snapshot();
            }
        }

        await SynchroniseScoreboardAsync(game, snapshot, cancellationToken).ConfigureAwait(false);
        await _games.UpdateAsync(game, cancellationToken).ConfigureAwait(false);

        return await BuildResponseAsync(game, snapshot, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Takes back the last move, or in computer mode the engine reply together with the human
    /// move before it. Undo remains available after the game is complete (Option B in the
    /// specification); reversing a finished game also takes its result back off the scoreboard.
    /// </summary>
    public async Task<GameStateResponse> UndoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var handle = await _locks.AcquireAsync(id, cancellationToken).ConfigureAwait(false);

        var game = await LoadAsync(id, cancellationToken).ConfigureAwait(false);

        var depth = GameEngine.UndoDepth(game.Mode, game.Moves);
        if (depth == 0)
        {
            throw new UndoNotAvailableException();
        }

        game.Moves.RemoveRange(game.Moves.Count - depth, depth);

        var snapshot = game.Snapshot();
        await SynchroniseScoreboardAsync(game, snapshot, cancellationToken).ConfigureAwait(false);
        await _games.UpdateAsync(game, cancellationToken).ConfigureAwait(false);

        return await BuildResponseAsync(game, snapshot, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Starts a fresh game in the same session: board and history cleared, status cleared,
    /// X to play. The scoreboard is deliberately left untouched, including any result this
    /// session already contributed.
    /// </summary>
    public async Task<GameStateResponse> ResetGameAsync(
        Guid id,
        GameMode? mode = null,
        CancellationToken cancellationToken = default)
    {
        using var handle = await _locks.AcquireAsync(id, cancellationToken).ConfigureAwait(false);

        var game = await LoadAsync(id, cancellationToken).ConfigureAwait(false);

        if (mode is not null)
        {
            game.Mode = mode.Value;
        }

        game.Clear();
        await _games.UpdateAsync(game, cancellationToken).ConfigureAwait(false);

        return await BuildResponseAsync(game, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads the session-level scoreboard.</summary>
    public async Task<ScoreboardResponse> GetScoreboardAsync(CancellationToken cancellationToken = default)
    {
        var scoreboard = await _scoreboards.GetAsync(cancellationToken).ConfigureAwait(false);
        return ResponseMapper.ToResponse(scoreboard);
    }

    /// <summary>Zeroes the scoreboard without touching any game in progress.</summary>
    public async Task<ScoreboardResponse> ResetScoreboardAsync(CancellationToken cancellationToken = default)
    {
        await _scoreboards.SaveAsync(Scoreboard.Empty, cancellationToken).ConfigureAwait(false);
        return ResponseMapper.ToResponse(Scoreboard.Empty);
    }

    /// <summary>
    /// Reconciles the scoreboard with the current result of a game. Because the game records
    /// which result it has already contributed, this is idempotent: repeated calls for an
    /// unchanged game do nothing, a newly finished game is counted exactly once, and a game
    /// whose result was undone has its contribution removed.
    /// </summary>
    private async Task SynchroniseScoreboardAsync(
        Game game,
        GameSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var current = snapshot.Result;
        if (current == game.RecordedResult)
        {
            return;
        }

        var scoreboard = await _scoreboards.GetAsync(cancellationToken).ConfigureAwait(false);

        if (game.RecordedResult is not null)
        {
            scoreboard = scoreboard.Revert(game.RecordedResult.Value);
        }

        if (current is not null)
        {
            scoreboard = scoreboard.Apply(current.Value);
        }

        await _scoreboards.SaveAsync(scoreboard, cancellationToken).ConfigureAwait(false);
        game.RecordedResult = current;
    }

    private async Task<Game> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _games.FindAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new GameNotFoundException(id);
    }

    private Task<GameStateResponse> BuildResponseAsync(Game game, CancellationToken cancellationToken) =>
        BuildResponseAsync(game, game.Snapshot(), cancellationToken);

    private async Task<GameStateResponse> BuildResponseAsync(
        Game game,
        GameSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var scoreboard = await _scoreboards.GetAsync(cancellationToken).ConfigureAwait(false);
        return ResponseMapper.ToResponse(game, snapshot, scoreboard);
    }
}
