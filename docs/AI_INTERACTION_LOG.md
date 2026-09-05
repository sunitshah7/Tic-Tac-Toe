# AI interaction log

A record of the conversation that produced this solution: what was asked, what was asked back,
what was decided, and what the assistant did in response. It exists because the problem
statement asks the candidate to be able to explain how the requirement became a specification,
what prompts were used, what the AI generated, what was changed, and what was reviewed
carefully.

**Tool:** Claude Code (Claude Opus 5), interactive session in VS Code
**Date:** 2026-09-05
**Repository:** `Tic-Tac-Toe` — empty at the start of the session apart from `LICENSE` and a
one-line `README.md`

---

## Turn 1 — the request

**Human** (with document attached: `Round 2 - Problem Statement.pdf`):

> Your task here is complete round problem statement. Create change log, context log for
> session, and create log of out interaction and you resspnse. Before starting it have you
> understand it? And do you have any other questions?

Read as: build the whole exercise; additionally produce three logs (a change log, a session
context log, and a log of this interaction); and — before starting — confirm understanding and
raise any questions.

**Assistant response.** Rather than answering "yes, I understand" from the documents alone,
the repository and toolchain were inspected first, so that the confirmation and the questions
would be grounded in what is actually installed:

- `git ls-files` — repository essentially empty, one commit on `main`.
- `dotnet --list-sdks` — 8.0.424.
- `node --version` / `npm --version` — v24.19.0 / 11.17.0.
- `ng version` — no global Angular CLI, so `npx` would be used.

Understanding was then stated concretely, as the shape of the intended solution rather than a
paraphrase of the PDF: a pure `GameEngine` domain class, a session store, a scoreboard service,
the seven suggested endpoints, a full game-state DTO, Swagger and CORS, xUnit tests covering
the specification's list; on the frontend, standalone Angular components with signals for the
board, status, history, scoreboard and the three buttons; and three documents in `docs/`
alongside a README with all twelve required sections.

**Four questions were then put to the human**, chosen because each changes the shape of the
work rather than a detail of it:

| Question | Options offered | Answer |
| --- | --- | --- |
| Clarification 2 — undo after completion | **B: allow undo, adjust the scoreboard** *(recommended)*; A: disable undo once complete | **Option B** |
| Storage | In-memory behind a port *(recommended)*; **SQLite + EF Core**; both implementations | **SQLite + EF Core** |
| Frontend test depth | **Focused component and service specs** *(recommended)*; add Playwright end-to-end; backend only | **Focused specs** |
| Where the logs live | **`docs/`, updated as the work progressed** *(recommended)*; repository root; one combined file | **`docs/`** |

Three of the four answers took the recommendation; the storage answer did not — the human chose
SQLite + EF Core over the recommended in-memory store. That preference is honoured throughout:
EF Core is the real persistence layer, not a stub, with entities separate from the domain model
and stores implementing domain-declared ports.

No further blocking questions were raised. Everything else that was ambiguous — whether the
scoreboard is per-session or global, whether the move request must name the player, whether
reset mints a new id — was resolved as a documented assumption rather than another round trip,
and each is listed in the README's *Clarifications and assumptions*.

---

## Turn 2 — the build

Everything below happened in a single continuous working turn, layer by layer, building and
testing at each step rather than generating the whole solution and debugging at the end.

### What the AI generated

**Backend, `TicTacToe.Domain`** — the rules, as pure functions with no framework references:
`GameEngine` (board construction, win detection over all eight lines, draw detection, turn
derivation, move validation, mode-dependent undo depth), `ComputerPlayer` (the specified
priority ladder), the `Game` aggregate, the `Scoreboard` value type, the supporting enums and
records, and the `IGameStore` / `IScoreboardStore` ports.

**Backend, `TicTacToe.Infrastructure`** — `GameDbContext` with separate persistence entities,
unique indexes on `(GameId, MoveNumber)` and `(GameId, CellIndex)`, a seeded singleton
scoreboard row, both store implementations with move reconciliation for undo, and the DI
extension.

**Backend, `TicTacToe.Api`** — the two controllers, the request/response contracts and mapper,
`GameService`, `GameSessionLocks`, the typed exception hierarchy, `GameExceptionHandler`, and
the `Program.cs` composition root with Swagger, CORS and string-serialised enums.

**Backend tests** — 81 across four files, covering every case in the specification's testing
list plus the HTTP status codes and problem-details shape.

**Frontend** — the Angular 22 application: typed models mirroring the wire contract, the REST
client, the signal store, three presentational components, the page shell, and the global
palette. Plus 41 tests across six files.

**Documentation** — the README's twelve sections, `docs/API.md`, and these three logs.

### What was changed manually, and what was reviewed carefully

**The row/column off-by-one.** The natural way to accept a row/column move is
`cellIndex = row * 3 + column`, then validate `0..8`. That silently accepts
`row 0, column 3` as cell 3 — an off-board coordinate folded onto a legal cell on the next row.
`ResolveCellIndex` was written to range-check `row` and `column` independently instead, and the
theory in `GameEndpointsTests` includes `(0, 3)` specifically to hold that behaviour. This is
the single most likely place for a subtle bug in the whole request path.

**A test that did not test what it claimed.** The first draft of `ComputerPlayerTests` had a
case called "takes any remaining cell when centre and corners are gone", asserting cell 1 on the
board `X.X.X.X.X`. Checking the assertion by hand showed it passes through the *block* rung, not
the fallback: with X on cells 0 and 2, cell 1 completes a line. Working the constraints through
further showed the fallback is unreachable in legal play at all — if the centre and all four
corners are occupied, some line always has two matching marks and a gap. The test was rebuilt on
a synthetic eight-mark board that genuinely reaches the fallback, and its comment now explains
why a real game cannot produce such a board. The lesson generalises: a passing test is not
evidence that the intended branch ran.

**Undo depth when the human's move ends the game.** The specification's computer-mode rule is
"remove the computer's last move and the human player's previous move together". Applied
literally to a game X has just won, that would remove X's winning move plus O's previous reply —
two moves, when the computer never got to answer. `GameEngine.UndoDepth` therefore keys off
which side played last: pair if the last move was the computer's, single otherwise. Both cases
are asserted.

**Draw and win fixtures verified by hand.** Every scripted move sequence in the tests was walked
through cell by cell to confirm it produces the intended outcome and, importantly, does not
produce an *earlier* accidental win that would make the test pass for the wrong reason.

**Scoreboard reconciliation rather than event handling.** The obvious implementation applies a
result when a game finishes and reverts it when an undo reverses one. That works until the two
paths disagree. Instead each game records which result it has contributed, and a single
reconciliation step compares current against recorded — making the operation idempotent, so no
sequence of moves, undos and resets can double-count or lose a result.

**Reading the scaffolded Angular version before writing specs.** `ng new` produced Angular 22
with Vitest and jsdom, not the Karma setup older Angular versions default to. The generated
`package.json` and `angular.json` were read before any spec was written, so the tests were
written for the runner that is actually there.

### How it was verified

Both suites were run (`dotnet test` → 81 passed; `ng test --no-watch` → 41 passed) and the
production build was checked (`ng build` → clean).

Passing tests do not prove the application starts, so the API was then run for real and driven
with `curl`: creating a computer-mode game, playing `row 0, column 0` and confirming the
computer's centre reply arrived in the same response with `undoDepth: 2`; the occupied-cell
rejection (409 with `errorCode: "CellOccupied"`) and the `column: 3` rejection (400); undo
removing the pair; a two-player win reporting `winningCells: [0,1,2]` and `xWins: 1`; the
Option B undo taking that win back off the scoreboard; reset clearing the board while preserving
the tallies; and the CORS preflight from `http://localhost:4200` returning 204 with the right
headers. The Angular dev server was started and confirmed to serve the shell. Both servers were
stopped afterwards.

---

## Assumptions and trade-offs, stated for the panel

**Assumptions** (each also in the README): the scoreboard is global to the API instance rather
than per game session; a move request must name its player so that "wrong player" is a rejection
the API can make; wire coordinates are 0-based while the displayed position string is 1-based;
the computer is always O and always second; `GET` never mutates; there is no authentication.

**Trade-offs:**

- *`EnsureCreated` over EF migrations* — one-command review setup, at the cost of schema
  evolution. Recorded as a limitation.
- *Whole state in every response* — a few extra bytes per call, in exchange for one render path
  in the client and no partial-update bugs.
- *In-process locking* — correct and simple for a single local instance; a multi-instance
  deployment would need a concurrency token instead.
- *Computer plays inside the human's request* — no half-finished turns and no reliance on client
  discipline, at the cost of a slightly less RESTful "one action, two moves" endpoint.
- *Deterministic opponent* — reproducible tests and demos, at the cost of variety across games.
- *Separate persistence entities from domain types* — a small amount of mapping code, in
  exchange for domain types that owe nothing to Entity Framework.

**What the AI did not decide.** The four questions above were escalated rather than assumed,
because each would have been expensive to reverse. Everything else was handled as ordinary
engineering judgment and written down.
