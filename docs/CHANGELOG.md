# Changelog

All notable changes to this project are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] — 2026-09-05

First complete implementation of the Round 2 problem statement: a browser-based Tic Tac Toe
game with an Angular front end and a .NET Web API back end, running locally.

### Added — backend

**Domain (`TicTacToe.Domain`)** — pure game rules with no framework references.

- `GameEngine`: board construction from a move list, win detection over all eight lines, draw
  detection, turn derivation, move validation, and mode-dependent undo depth.
- `ComputerPlayer`: the specified priority ladder — win, block, centre, corner, any free cell —
  with deterministic tie-breaking on the lowest cell index.
- `Game` aggregate storing only id, mode, ordered moves and the result already contributed to
  the scoreboard; every other property is derived.
- `GameSnapshot`, `PlacedMove`, `Scoreboard` value types; `Player`, `GameMode`, `GameStatus`,
  `GameResult` and `MoveRejectionReason` enums.
- `IGameStore` and `IScoreboardStore` persistence ports.

**Infrastructure (`TicTacToe.Infrastructure`)** — EF Core 8 over SQLite.

- `GameDbContext` with game, move and scoreboard entities, unique indexes on
  `(GameId, MoveNumber)` and `(GameId, CellIndex)`, and a seeded singleton scoreboard row.
- `GameStore` and `ScoreboardStore` implementing the domain ports, including move reconciliation
  so that both appended moves and undo truncations persist.
- `AddInfrastructure` DI extension taking a connection string.

**API (`TicTacToe.Api`)**

- `GamesController`: create, read, move, undo and reset endpoints.
- `ScoreboardController`: read and reset endpoints.
- `GameService`: application orchestration — persistence, the automatic computer reply, and
  idempotent scoreboard reconciliation.
- `GameSessionLocks`: per-game semaphores serialising read-modify-write mutations.
- Request and response contracts, with a backend-formatted `position` string
  ("Row 1, Column 1") so every client renders the history identically.
- `GameExceptionHandler` mapping typed rule violations to RFC 7807 problem details with a stable
  `errorCode`; 400 for malformed requests, 409 for state conflicts, 404 for unknown sessions.
- Swagger/OpenAPI with XML documentation, CORS for the Angular dev origin, string-serialised
  enums, and schema creation on startup.

### Added — frontend

- Angular 22 standalone application using signals and zoneless change detection.
- `GameApiService`: typed REST client covering all seven endpoints.
- `GameStore`: signal-based UI state holding the last state the backend reported, plus busy and
  error signals; it deliberately derives no game rules of its own.
- `GameBoardComponent`: 3 × 3 grid with locked cells, winning-cell highlighting and
  screen-reader labels.
- `MoveHistoryComponent`: move number, player and position table.
- `ScoreboardPanelComponent`: X/O/draw tallies with the Reset Scoreboard action.
- Page shell with mode selector, status line, Reset Game and Undo Last Move buttons, and an
  error banner carrying the backend's own explanation.
- Responsive two-column layout that stacks below laptop width, with light and dark palettes.

### Added — tests

- 81 backend tests: `GameEngineTests` (valid and invalid moves, turn switching, row/column/
  diagonal wins as theories over all eight lines, draw, undo depth), `ComputerPlayerTests` (one
  test per rung of the ladder), `ScoreboardTests`, `GameServiceTests` (state transitions over a
  real in-memory SQLite database) and `GameEndpointsTests` (HTTP status codes and problem
  details against the API hosted in process).
- 41 frontend tests: REST client request shapes, store behaviour including error handling and
  mode switching, and component rendering for board, history, scoreboard and shell.

### Added — documentation

- `README.md` covering all twelve required sections.
- `docs/API.md`: the full REST contract with worked examples.
- `docs/CONTEXT_LOG.md`: session context, decisions and verification record.
- `docs/AI_INTERACTION_LOG.md`: the prompts, answers and responses behind the build.
- `.gitignore` for .NET, Node and the local SQLite database.

### Decisions recorded

- **Clarification 2 resolved as Option B**: undo remains available after a completed game, and
  reversing a result adjusts the scoreboard back.
- **SQLite + EF Core** chosen over in-memory storage, behind domain-declared ports.
- Schema created with `EnsureCreated` rather than migrations, so review needs only `dotnet run`.
- Reset keeps the session id and mode switching goes through the same endpoint.

### Fixed during development

- `GamesController.ResolveCellIndex` range-checks `row` and `column` independently. Computing
  `row * 3 + column` and validating only the 0–8 result accepts `row 0, column 3` as the legal
  cell 3; the theory case `(0, 3)` now guards this.
- `ComputerPlayerTests` no longer claims to cover the "take any available cell" rung on a board
  that actually exercises the block rung. With the centre and all four corners occupied some
  line always has two matching marks and a gap, so the fallback is unreachable in legal play;
  the test now uses a synthetic board that reaches it and says why a game cannot produce one.
