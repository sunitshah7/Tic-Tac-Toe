using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

/// <summary>
/// Session-level scoreboard endpoints. The scoreboard is owned by the backend and spans
/// every game played against this instance.
/// </summary>
[ApiController]
[Route("api/scoreboard")]
[Produces("application/json")]
public sealed class ScoreboardController : ControllerBase
{
    private readonly GameService _games;

    public ScoreboardController(GameService games) => _games = games;

    /// <summary>Reads the current tallies.</summary>
    /// <response code="200">X wins, O wins and draws.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ScoreboardResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScoreboardResponse>> Get(CancellationToken cancellationToken) =>
        Ok(await _games.GetScoreboardAsync(cancellationToken));

    /// <summary>Zeroes the tallies. Games in progress are unaffected.</summary>
    /// <response code="200">The cleared scoreboard.</response>
    [HttpPost("reset")]
    [ProducesResponseType(typeof(ScoreboardResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScoreboardResponse>> Reset(CancellationToken cancellationToken) =>
        Ok(await _games.ResetScoreboardAsync(cancellationToken));
}
