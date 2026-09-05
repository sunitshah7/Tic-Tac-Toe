# Tic Tac Toe — Angular + .NET

A browser-based Tic Tac Toe game with an Angular front end and a .NET Web API back end,
both running locally. The backend owns the game rules, the move history and the scoreboard;
the frontend renders whatever the backend says the state is and never decides a move for itself.

---

## 1. Project overview

Two people (or one person and a basic computer opponent) play Tic Tac Toe in the browser.
Every click becomes a REST call. The API validates the move against the current game, applies
it, plays the computer's reply when in computer mode, updates the scoreboard when a game
finishes, and returns the complete new game state. The Angular app re-renders from that
response.

The design principle running through the whole solution is **one source of truth**. The
frontend holds no board, no turn counter and no win detection of its own. The backend holds
no derived state: a game row stores its ordered move list and nothing else, and the board,
current player, status, winner and winning cells are recomputed from that list on every read.
That is what makes Undo a truncation rather than an inverse operation, and it is why the board
on screen can never disagree with the board in the database.

```
Angular (signals)  ──HTTP/JSON──▶  ASP.NET Core Web API  ──EF Core──▶  SQLite file
      ▲                                     │
      └──────────── full game state ────────┘
```

---

## 2. Tech stack

| Layer | Choice |
| --- | --- |
| Frontend | Angular 22, TypeScript 6, standalone components, signals, zoneless change detection |
| Frontend tests | Vitest + jsdom (the Angular CLI's `@angular/build:unit-test` builder) |
| Backend | .NET 8, ASP.NET Core Web API, controllers |
| Persistence | Entity Framework Core 8 over SQLite |
| Backend tests | xUnit, plus `WebApplicationFactory` for in-process API tests |
| API docs | Swagger / OpenAPI (Swashbuckle), served at `/swagger` in Development |
| Source control | Git / GitHub |

### Solution layout

```
backend/
  src/TicTacToe.Domain/          Pure game rules. No framework references at all.
  src/TicTacToe.Infrastructure/  EF Core DbContext, entities, store implementations.
  src/TicTacToe.Api/             Controllers, DTOs, application service, composition root.
  tests/TicTacToe.Tests/         xUnit: domain, service and HTTP-level tests.
frontend/
  src/app/models/                TypeScript mirror of the wire contract.
  src/app/services/              REST client and the signal store.
  src/app/components/            Board, move history, scoreboard panel.
  src/app/app.*                  Page shell that wires them together.
docs/                            Changelog, session context log, AI interaction log, API reference.
```

The three backend projects give a clean dependency direction: `Domain` knows nothing,
`Infrastructure` knows `Domain`, `Api` knows both. The persistence ports (`IGameStore`,
`IScoreboardStore`) are declared in `Domain` and implemented in `Infrastructure`, so swapping
SQLite for anything else touches two classes and no rules.

---

## 3. Features implemented

**Game board**
- Standard 3 × 3 board; empty cells are clickable, taken cells are locked for the rest of the game.
- Cells render X or O and are labelled for screen readers ("Row 1, Column 1, X").

**Player turns**
- Player X and Player O, with the current player shown above the board.
- Turns alternate after every valid move; a rejected move leaves the turn exactly where it was.

**Win and draw detection**
- Rows, columns and both diagonals.
- The winner is announced, the three winning cells are highlighted, further moves are refused,
  and the scoreboard is updated once.
- A full board with no line is a draw, with the same consequences.

**Reset Game**
- Clears the board, the move history and the win/draw status, sets the turn back to X, and
  leaves the scoreboard untouched.

**Move history**
- Move number, player and cell position for the current game, updated after every valid move.
- The position string ("Row 2, Column 2") is formatted by the backend so every client agrees.

**Undo Last Move**
- Two Player mode: removes the single most recent move.
- Computer mode: removes the computer's reply together with the human move that provoked it,
  returning the turn to X.
- Restores the board, the turn and the win/draw status, keeps the history accurate, and is
  disabled when there is nothing to undo.
- Available after a completed game — see [Clarification 2](#10-clarifications-and-assumptions).

**Scoreboard**
- Session-level X wins / O wins / Draws, served by the backend and shared across games.
- Updated exactly once per completed game; unaffected by Reset Game; cleared only by the
  separate Reset Scoreboard action.

**Game modes**
- Two Player, and Play Against Computer (human is X, computer is O).
- The computer replies automatically in the same HTTP response as the human move, plays only
  legal moves, and does not move once the game is over.
- Its priority ladder: win if possible → block X → centre → corner → any free cell.

---

## 4. How to run the backend locally

Prerequisite: **.NET SDK 8.0** (`dotnet --version` should print 8.x).

```bash
cd backend/src/TicTacToe.Api
dotnet run
```

The API listens on **http://localhost:5090** and opens Swagger UI at
<http://localhost:5090/swagger>.

A SQLite file `tictactoe.db` is created next to the project on first run — no migration step,
no setup. Delete the file to start from a clean slate. It is git-ignored.

To use a different port or database:

```bash
dotnet run --urls http://localhost:5099
# or edit ConnectionStrings:GameDatabase in appsettings.json
```

If you change the port, update `frontend/src/environments/environment.ts` to match, and add the
frontend origin to `Cors:AllowedOrigins` in `appsettings.json` if you also move the frontend.

---

## 5. How to run the frontend locally

Prerequisites: **Node.js 20.19+, 22.12+ or 24+** and npm. (Developed on Node 24.19.)

```bash
cd frontend
npm install     # first time only
npm start
```

The app is served at **http://localhost:4200** and talks to the API at
`http://localhost:5090/api`. Start the backend first; if it is not running, the UI says so
rather than failing silently.

---

## 6. API endpoint summary

Base URL: `http://localhost:5090/api`. Full request/response detail is in
[docs/API.md](docs/API.md) and in Swagger.

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `POST` | `/api/games` | Create a game session (`{ "mode": "TwoPlayer" \| "Computer" }`) |
| `GET` | `/api/games/{id}` | Read the current game state |
| `POST` | `/api/games/{id}/moves` | Submit a move |
| `POST` | `/api/games/{id}/undo` | Undo the last move, or move pair in computer mode |
| `POST` | `/api/games/{id}/reset` | Clear the board and history; optionally switch mode |
| `GET` | `/api/scoreboard` | Read the session scoreboard |
| `POST` | `/api/scoreboard/reset` | Zero the scoreboard |

Every game endpoint returns the same **game state** shape:

```jsonc
{
  "id": "c6a9f958-...",
  "mode": "Computer",
  "board": ["X", null, null, null, "O", null, null, null, null],
  "currentPlayer": "X",           // null once the game is complete
  "status": "InProgress",         // InProgress | Won | Draw
  "winner": null,                 // "X" | "O" when status is Won
  "winningCells": [],             // e.g. [0, 1, 2] - the cells to highlight
  "moves": [
    { "moveNumber": 1, "player": "X", "row": 0, "column": 0,
      "cellIndex": 0, "position": "Row 1, Column 1" }
  ],
  "canUndo": true,
  "undoDepth": 2,                 // how many moves the next undo removes
  "scoreboard": { "xWins": 0, "oWins": 0, "draws": 0 }
}
```

A move request names the player and the target cell, as either a flat index or a row/column pair:

```jsonc
{ "player": "X", "cellIndex": 4 }
{ "player": "X", "row": 1, "column": 1 }   // equivalent
```

Rejections come back as RFC 7807 problem details with a stable `errorCode`:

| Situation | Status | `errorCode` |
| --- | --- | --- |
| Unknown game id | 404 | `GameNotFound` |
| Neither `cellIndex` nor row + column supplied | 400 | `InvalidMoveRequest` |
| Cell outside the board | 400 | `OutOfBoard` |
| Cell already taken | 409 | `CellOccupied` |
| Game already won or drawn | 409 | `GameCompleted` |
| Not that player's turn | 409 | `WrongPlayer` |
| Client tried to play O in computer mode | 409 | `NotHumanControlled` |
| Undo with no moves left | 409 | `NothingToUndo` |

The 400/409 split is deliberate: 400 means *you asked wrongly*, 409 means *you asked at the
wrong time*. The Angular client shows the `detail` sentence and keeps the last known good state.

---

## 7. How to run tests

**Backend** — 81 tests: domain rules, computer move selection, scoreboard arithmetic, service
state transitions over a real in-memory SQLite database, and HTTP-level tests against the API
hosted in process.

```bash
cd backend
dotnet test
```

**Frontend** — 41 tests: the REST client's request shapes, the signal store's behaviour
(including error handling), and component rendering for the board, move history, scoreboard and
page shell.

```bash
cd frontend
npm test -- --no-watch     # or: npx ng test --no-watch
```

Coverage against the specification's minimum list:

| Required case | Where |
| --- | --- |
| Valid move | `GameEngineTests.ValidMove_IsAccepted_AndMarksTheCell`, `GameServiceTests.SubmitMove_PlacesTheMark_AndPassesTheTurn` |
| Invalid move | `GameEngineTests` (out of board, occupied, wrong player, after completion), `GameEndpointsTests` for the status codes |
| Turn switching | `GameEngineTests.Turns_AlternateAfterEveryValidMove` |
| Row / column / diagonal win | `GameEngineTests.RowWin…`, `ColumnWin…`, `DiagonalWin…` (theories over all eight lines) |
| Draw | `GameEngineTests.FullBoardWithNoLine_IsADraw`, `GameServiceTests.Draw_IsDetected_AndCounted` |
| Reset game | `GameServiceTests.ResetGame_ClearsTheBoardAndHistory_ButKeepsTheScoreboard` |
| Undo in two-player mode | `GameServiceTests.Undo_InTwoPlayerMode_RemovesOnlyTheLastMove` |
| Undo in computer mode | `GameServiceTests.Undo_InComputerMode_RemovesTheMovePair_AndReturnsTheTurnToX` |
| Scoreboard update | `GameServiceTests.Win_ReportsTheWinnerAndHighlightCells_AndScoresOnce`, `Undo_AfterCompletion_ReversesTheResultOnTheScoreboard` |
| Computer move selection | `ComputerPlayerTests` (one test per rung of the priority ladder) |
| Move after game completion | `GameServiceTests.SubmitMove_AfterCompletion_IsRejected`, `GameEndpointsTests.SubmitMove_AfterCompletion_Returns409` |

---

## 8. AI tools and prompt summary

Built with **Claude Code (Claude Opus 5)** in an interactive session. The full transcript —
what was asked, what was answered, what was generated and what was corrected — is in
[docs/AI_INTERACTION_LOG.md](docs/AI_INTERACTION_LOG.md); the decision trail is in
[docs/CONTEXT_LOG.md](docs/CONTEXT_LOG.md).

In short:

1. **Specification first.** The problem-statement PDF was read and restated as a concrete
   build plan (projects, endpoints, component list, test list) before any code was written.
2. **Open questions surfaced up front.** Four decisions the specification leaves to the
   candidate — the undo-after-completion policy, storage, frontend test depth, and where the
   logs live — were put to the human as explicit choices rather than assumed. The answers were
   Option B, SQLite + EF Core, focused component/service specs, and a `docs/` folder.
3. **Layer by layer, compiling at each step.** Domain → Infrastructure → API → tests →
   frontend → tests → docs, building and running the suite after each layer rather than
   generating everything and debugging at the end.
4. **Verified by running it,** not just by passing tests: the API was started and exercised with
   `curl` for the computer-mode reply, both invalid-move families, undo, the Option B scoreboard
   reversal, reset semantics and the CORS preflight, and the Angular dev server was started and
   confirmed to serve.

Things that were reviewed carefully rather than accepted as generated, and the manual
corrections that followed, are listed in the AI interaction log. The two worth naming here:

- The row/column form of a move request needed **separate range checks**. Computing
  `row * 3 + column` and then testing `0..8` silently accepts `row 0, column 3` as cell 3 —
  a real off-by-one that maps an off-board coordinate onto a legal cell. Fixed in
  `GamesController.ResolveCellIndex`, and covered by a theory including `(0, 3)`.
- A first draft of the computer-opponent tests claimed to cover the "take any available cell"
  fallback on a board that in fact exercised the *block* rung. Working through it showed that
  with the centre and all four corners occupied, some line always has two matching marks and a
  gap, so that rung is unreachable in legal play. The test was rebuilt on a synthetic board
  that genuinely reaches it, and the comment now says why such a board cannot occur in a game.

---

## 9. Design decisions

**The move list is the only stored state.** A game row holds its id, mode, ordered moves and
one field recording which result it has already contributed to the scoreboard. Board, turn,
status, winner and winning cells are derived by `GameEngine.Evaluate` on every read. Undo
becomes `RemoveRange` at the tail; there is no inverse-move logic to get wrong, and no way for
a stored board to contradict a stored history.

**Rules live in a project with no framework references.** `TicTacToe.Domain` has no EF Core, no
ASP.NET, no DI. Every rule in the specification is a pure function, which is why the rule tests
run in milliseconds and read like the specification itself.

**The API returns the whole state from every action.** One shape, one render path in the client.
It costs a few extra bytes per response and removes an entire class of partial-update bugs.

**The computer plays inside the human's request.** `POST /moves` in computer mode applies X's
move and O's reply and returns both. The alternative — a separate "computer move" endpoint —
would let a client leave a game parked mid-turn, and would make Undo's "remove the pair"
behaviour depend on client discipline.

**The computer is deterministic.** Among equally ranked candidates it always takes the lowest
cell index. A random tie-break would play no better and would make both the tests and a panel
demo unrepeatable.

**Reset keeps the session id.** `POST /reset` clears the board and history in place rather than
minting a new id. The session is the table the players are sitting at; the games come and go
around it, and the scoreboard belongs to the table. It also means the client has no id to
re-bind after every reset. Switching mode goes through the same endpoint, because a
half-played two-player game has no sensible reading once O becomes the computer.

**Errors are typed, not stringly.** Rule violations are exceptions carrying a
`MoveRejectionReason`; one `IExceptionHandler` maps them to problem details with a stable
`errorCode`. Controllers state only the happy path.

**Mutations are serialised per game.** Every mutation is a read-modify-write over the move list,
so `GameSessionLocks` holds a semaphore per game id. In-process locking is right for a
single-instance local app; a multi-instance deployment would need optimistic concurrency on a
version column instead.

**The frontend keeps UI state only.** `GameStore` holds the last state the backend sent, an
error message and a busy flag. It has no board logic and no rules. A failed request leaves the
previous good state on screen and shows the server's sentence.

---

## 10. Clarifications and assumptions

**Clarification 1 — backend state ownership.** The backend is the source of truth. Move
validation, status, history and scoreboard all come from the API; the frontend's only local
state is "what did the server last tell me", plus loading and error flags.

**Clarification 2 — scoreboard and undo: Option B (allow undo after completion).**
Undo stays available after a win or a draw. Each game records which result it has already
contributed; when an undo reverses a completed game, that contribution is taken back off the
scoreboard and the game returns to `InProgress`. Replaying the winning move counts it once
again. The bookkeeping is idempotent — reconciliation compares the game's current result with
its recorded one, so nothing is ever double-counted, and tallies are clamped at zero.

Assumptions made where the specification is silent:

- **The scoreboard is global to the API instance**, not per game session. "Session-level" is
  read as the play session against this backend, so several game sessions share one scoreboard.
  Reset Scoreboard clears it for everyone.
- **The move request must name the player.** It is not inferred from whose turn it is, so that
  "move by the wrong player" is a rejection the API can actually make.
- **Board coordinates are 0-based on the wire and 1-based in the UI.** `row`/`column`/`cellIndex`
  are 0-based in JSON; the `position` string the backend formats is 1-based ("Row 1, Column 1")
  to match the specification's example table.
- **The computer always plays O and always moves second.** The specification fixes this; there
  is no "computer plays X" mode.
- **`GET` requests never change state.** In particular, reading a completed game does not
  re-score it.
- **No authentication.** Anyone who can reach the port can play; appropriate for a local
  exercise, and called out under limitations.
- **`EnsureCreated` instead of EF migrations.** The schema is created on first run so a reviewer
  needs only `dotnet run`. A real project would use migrations; see limitations.

---

## 11. Known limitations

- **No authentication or authorisation.** Any client that can reach the API can read or mutate
  any game by id. Game ids are GUIDs, which is obscurity, not security.
- **`EnsureCreated`, not migrations.** Changing the model means deleting `tictactoe.db`. The
  trade was reviewer convenience over schema evolution; `dotnet ef migrations add` plus
  `Database.Migrate()` is the production answer.
- **Per-process locking.** `GameSessionLocks` protects a single instance. Behind a load balancer
  two nodes could interleave writes to one game; that needs a concurrency token in the database.
- **Sessions are never cleaned up.** Rows accumulate in the SQLite file forever. There is no TTL
  and no delete endpoint.
- **The computer is a fixed heuristic, not a solver.** It follows the specified ladder, which is
  strong but beatable — it takes the first corner rather than reasoning about forks, so a human
  playing opposite corners can set up a double threat and win.
- **No end-to-end browser tests.** Component and API-integration tests are in place; a
  Playwright suite driving a real browser against both servers is not.
- **The scoreboard cannot be attributed.** It counts X and O wins, not *whose*, so the tallies
  mean less in computer mode than in two-player mode.
- **No optimistic-concurrency signalling to the client.** If two tabs play the same game, the
  second one's move is simply applied or rejected on its merits; there is no version conflict
  response.
- **Undo has no redo**, and the history cannot be replayed forward once truncated.

---

## 12. Future improvements

- **EF Core migrations** plus a seeded schema check on startup, replacing `EnsureCreated`.
- **A concurrency token** (`RowVersion`) on the game row, so the per-process lock can go and
  the API can scale out honestly.
- **SignalR** for a shared game across two browsers — the state-per-response design already
  suits push, since every mutation produces a complete state to broadcast. This is also the
  closest analogue to the real-time IT/OT streaming the role description describes.
- **Difficulty levels**: keep the current ladder as "Basic" and add a minimax opponent as
  "Unbeatable", which the pure-function engine makes straightforward.
- **Per-player scoreboards and game history**, with named players and a completed-games list.
- **Playwright end-to-end tests** covering a full two-player game, a computer game, undo in both
  modes, and both reset buttons, run in CI against both servers.
- **A CI pipeline** (GitHub Actions) running `dotnet test` and `ng test` on every push, with
  coverage thresholds.
- **Containerisation**: a `docker compose` file bringing up API and frontend together, so a
  reviewer runs one command instead of two.
- **Health and telemetry**: `/health`, structured logging and OpenTelemetry traces across the
  API call path.

---

## Repository documents

| Document | What it holds |
| --- | --- |
| [docs/API.md](docs/API.md) | Full REST contract: every endpoint, body, response and error |
| [docs/CHANGELOG.md](docs/CHANGELOG.md) | What was built, in the order it was built |
| [docs/CONTEXT_LOG.md](docs/CONTEXT_LOG.md) | Session context: decisions, assumptions, verification |
| [docs/AI_INTERACTION_LOG.md](docs/AI_INTERACTION_LOG.md) | The prompts, the answers and the responses |
