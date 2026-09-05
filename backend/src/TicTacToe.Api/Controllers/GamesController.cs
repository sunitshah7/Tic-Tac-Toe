using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Services;
using TicTacToe.Domain;

namespace TicTacToe.Api.Controllers;

/// <summary>Game session endpoints. Every action returns the complete game state.</summary>
[ApiController]
[Route("api/games")]
[Produces("application/json")]
public sealed class GamesController : ControllerBase
{
    private readonly GameService _games;

    public GamesController(GameService games) => _games = games;

    /// <summary>Creates a new game session.</summary>
    /// <response code="201">The new session, with an empty board and X to play.</response>
    [HttpPost]
    [ProducesResponseType(typeof(GameStateResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<GameStateResponse>> CreateGame(
        [FromBody] CreateGameRequest? request,
        CancellationToken cancellationToken)
    {
        var mode = request?.Mode ?? GameMode.TwoPlayer;
        var state = await _games.CreateGameAsync(mode, cancellationToken);
        return CreatedAtAction(nameof(GetGame), new { id = state.Id }, state);
    }

    /// <summary>Reads the current state of a game session.</summary>
    /// <response code="200">The current state.</response>
    /// <response code="404">No session with that id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GameStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameStateResponse>> GetGame(Guid id, CancellationToken cancellationToken) =>
        Ok(await _games.GetGameAsync(id, cancellationToken));

    /// <summary>
    /// Submits a move. In computer mode the engine reply is played in the same request, so
    /// the returned state already includes it.
    /// </summary>
    /// <response code="200">The state after the move (and the computer reply, if any).</response>
    /// <response code="400">The request did not identify a cell on the board.</response>
    /// <response code="404">No session with that id.</response>
    /// <response code="409">The move broke a game rule; see <c>errorCode</c>.</response>
    [HttpPost("{id:guid}/moves")]
    [ProducesResponseType(typeof(GameStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GameStateResponse>> SubmitMove(
        Guid id,
        [FromBody] SubmitMoveRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Player is null)
        {
            throw new InvalidMoveRequestException("The move request must name the player.");
        }

        var cellIndex = ResolveCellIndex(request);
        var state = await _games.SubmitMoveAsync(id, request.Player.Value, cellIndex, cancellationToken);
        return Ok(state);
    }

    /// <summary>
    /// Undoes the last move, or in computer mode the computer reply together with the human
    /// move that preceded it.
    /// </summary>
    /// <response code="200">The restored state.</response>
    /// <response code="404">No session with that id.</response>
    /// <response code="409">There are no moves to undo.</response>
    [HttpPost("{id:guid}/undo")]
    [ProducesResponseType(typeof(GameStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GameStateResponse>> Undo(Guid id, CancellationToken cancellationToken) =>
        Ok(await _games.UndoAsync(id, cancellationToken));

    /// <summary>
    /// Starts a fresh game in this session. The scoreboard is left unchanged. An optional
    /// mode switches the session between Two Player and Computer at the same time.
    /// </summary>
    /// <response code="200">The cleared state.</response>
    /// <response code="404">No session with that id.</response>
    [HttpPost("{id:guid}/reset")]
    [ProducesResponseType(typeof(GameStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameStateResponse>> Reset(
        Guid id,
        [FromBody] CreateGameRequest? request,
        CancellationToken cancellationToken) =>
        Ok(await _games.ResetGameAsync(id, request?.Mode, cancellationToken));

    /// <summary>
    /// Works out which cell the request refers to. A flat index wins when both forms are
    /// supplied; row and column are range-checked separately so that, say, column 3 is
    /// rejected as off-board rather than silently wrapping onto the next row.
    /// </summary>
    private static int ResolveCellIndex(SubmitMoveRequest request)
    {
        if (request.CellIndex is not null)
        {
            return request.CellIndex.Value;
        }

        if (request.Row is null || request.Column is null)
        {
            throw new InvalidMoveRequestException(
                "The move request must supply either cellIndex, or both row and column.");
        }

        var (row, column) = (request.Row.Value, request.Column.Value);
        if (row < 0 || row >= GameEngine.Size || column < 0 || column >= GameEngine.Size)
        {
            throw new MoveRejectedException(MoveRejectionReason.OutOfBoard);
        }

        return GameEngine.ToCellIndex(row, column);
    }
}
