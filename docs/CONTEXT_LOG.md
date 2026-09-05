# Session context log

A running record of the context this solution was built in: what the task was, what was
decided and why, what was verified and how, and what a future session (or a panel discussion)
needs to know to pick the work up cleanly.

**Session date:** 2026-09-05
**Repository:** `Tic-Tac-Toe`, branch `main`
**Starting state:** empty repository — `LICENSE` and a one-line `README.md`, one commit.
**Tooling present:** .NET SDK 8.0.424, Node v24.19.0, npm 11.17.0. No global Angular CLI (used
via `npx`), no `dotnet-ef` tool.

---

## 1. The task

Two documents were supplied: the Round 2 problem statement (a full-stack Tic Tac Toe exercise)
and the job description for Principal Software Engineer – Full Stack at ABB.

The problem statement asks for a browser Tic Tac Toe with an Angular front end and a .NET Web
API back end, running locally, where the backend owns the game session and scoreboard state.
Beyond the base game it requires move history, mode-dependent undo, a session scoreboard and a
basic computer opponent, plus tests, a documented API contract and a README with twelve
specific sections.

The job description is context rather than requirement, but it shapes emphasis: it asks for
SOLID design, clean layering, high test coverage, well-structured REST APIs, and explicitly for
engineers who can integrate AI-assisted workflows into the SDLC and explain the result. That is
why this solution puts effort into layer separation, typed error handling and a documented AI
trail rather than only into making the game work.

The human's request added three deliverables on top of the specification: a change log, a
context log for the session (this file), and a log of the interaction and responses.

---

## 2. Decisions taken, and by whom

Four decisions the specification leaves open were put to the human before any code was written,
because each one changes the shape of the work rather than just its details.

| Decision | Choice | Rationale |
| --- | --- | --- |
| Clarification 2: undo after completion | **Option B** — allow undo, adjust the scoreboard | The harder of the two options, and the one that gives a panel something to discuss; it forces explicit bookkeeping of which result each game has contributed |
| Storage | **SQLite + EF Core** | The specification permits in-memory; EF Core is named in the job description, and persistence across restarts is a better demonstration |
| Frontend test depth | **Focused component and service specs** | Matches "frontend tests may cover component rendering and API integration points"; end-to-end browser tests were left as a documented future improvement |
| Location of the three logs | **`docs/` folder, written as the work progressed** | Keeps them honest records rather than a retrospective reconstruction, and keeps the repository root readable |

Decisions taken without asking, as ordinary engineering judgment:

- **Three backend projects** (Domain / Infrastructure / Api) rather than one. The dependency
  direction is the point: rules that cannot reference a framework cannot drift towards one.
- **Move list as the only stored state.** Everything else is derived. This is the decision that
  most shapes the codebase — see the design notes in the README.
- **Deterministic computer opponent.** Lowest index among equal candidates, so tests and demos
  are reproducible.
- **Reset keeps the session id.** The session is the table; games come and go around it.
- **400 vs 409 split** on rejections: malformed request vs wrong moment.
- **`EnsureCreated` over migrations**, trading schema evolution for a one-command review setup.
  Recorded as a known limitation.

---

## 3. Build order

The work proceeded layer by layer, compiling and testing at each step rather than generating
everything and debugging at the end.

1. Repository and toolchain inspection; NuGet and npm reachability confirmed before committing
   to a stack that needs both.
2. Backend solution scaffolded: four projects, references wired, EF Core SQLite and
   `Microsoft.AspNetCore.Mvc.Testing` added.
3. Domain layer written and built in isolation — enums, `PlacedMove`, `GameSnapshot`,
   `GameEngine`, `ComputerPlayer`, `Scoreboard`, `Game`, and the two persistence ports.
4. Infrastructure: entities, `GameDbContext`, the two stores, the DI extension.
5. API: contracts and mapper, `GameService`, `GameSessionLocks`, typed exceptions, the exception
   handler, both controllers, and `Program.cs` composition.
6. Backend tests written and run — 81 passing on the first full run.
7. Angular application scaffolded (`ng new` produced Angular 22 with Vitest, not the Karma setup
   that older Angular versions default to; the specs were written for Vitest accordingly).
8. Frontend: models, REST client, signal store, three components, page shell, global styles.
9. Frontend tests written and run — 41 passing.
10. End-to-end verification against the running servers.
11. Documentation and logs.

---

## 4. Verification performed

Automated:

- `dotnet test` — 81 passed, 0 failed.
- `npx ng test --no-watch` — 41 passed across 6 files.
- `npx ng build` — clean.

Manual, against the actually running system (this matters: passing tests do not prove the app
starts):

- API started on `http://localhost:5090`; `GET /api/scoreboard` answered.
- Computer mode: `POST /moves` with `row: 0, column: 0` returned X at cell 0 **and** O at cell 4
  in one response, `currentPlayer: "X"`, `undoDepth: 2`.
- Invalid moves: occupied cell → `409 CellOccupied` with problem details; `column: 3` → `400`
  (the off-by-one guard doing its job).
- Undo in computer mode removed both moves and returned an empty board with X to play.
- Two-player win sequence: `status: "Won"`, `winner: "X"`, `winningCells: [0,1,2]`,
  `xWins: 1`.
- Option B undo after that win: status back to `InProgress`, `winningCells` empty, `xWins`
  back to `0`.
- Reset game: board and history cleared, scoreboard preserved.
- CORS preflight from `http://localhost:4200` returned `204` with the expected
  `Access-Control-Allow-*` headers.
- Angular dev server on `http://localhost:4200` served the application shell.
- Both servers stopped afterwards; the local `tictactoe.db` is git-ignored.

---

## 5. Two things worth reviewing carefully

Recorded here because they are the places where a reviewer's attention is best spent, and
because both were caught by reasoning about the code rather than by a failing test.

**Row/column range checking.** `GamesController.ResolveCellIndex` validates `row` and `column`
independently before converting to a flat index. Converting first and then checking `0..8`
would accept `row 0, column 3` as cell 3 — an off-board coordinate silently mapped onto a legal
cell. The theory case `(0, 3)` in `GameEndpointsTests` exists specifically to hold this.

**The unreachable rung of the computer's ladder.** A first draft of `ComputerPlayerTests`
claimed to cover "take any available cell" using a board that in fact exercised the block rung.
Working through the constraints shows that if the centre and all four corners are occupied,
some line always has two matching marks and a gap — so the fallback cannot be reached in legal
play. The test now uses a synthetic board that genuinely reaches it, and its comment explains
why a real game never produces one. The branch stays in the code as a defensive default.

---

## 6. State at the end of the session

Complete and verified against every item in the specification's acceptance criteria. Both
applications run locally, the frontend talks to the backend over REST, all functional
requirements and must-haves are implemented, tests cover the required list, and the README
explains how to run and review the solution.

Not done, deliberately, and documented as limitations or future improvements: end-to-end
browser tests, EF migrations, multi-instance concurrency handling, authentication, session
cleanup, and a CI pipeline.

Nothing is left half-finished. There is no work in progress to hand over.
