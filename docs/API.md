# API contract

Base URL (local development): `http://localhost:5090/api`
Interactive documentation: <http://localhost:5090/swagger> while the API runs in Development.

All requests and responses are `application/json`. Enums travel as their names, not integers.
Board coordinates on the wire are **0-based** (`row` and `column` are 0–2, `cellIndex` is 0–8,
row-major); the `position` display string is 1-based to match the specification's example table.

---

## Shared shapes

### GameState

Returned by every endpoint under `/api/games`.

| Field | Type | Notes |
| --- | --- | --- |
| `id` | `string` (GUID) | Session identifier |
| `mode` | `"TwoPlayer" \| "Computer"` | Selected game mode |
| `board` | `("X" \| "O" \| null)[]` | Exactly 9 entries, row-major; `null` is empty |
| `currentPlayer` | `"X" \| "O" \| null` | `null` once the game is complete |
| `status` | `"InProgress" \| "Won" \| "Draw"` | |
| `winner` | `"X" \| "O" \| null` | Set only when `status` is `Won` |
| `winningCells` | `number[]` | The three indices to highlight; empty otherwise |
| `moves` | `GameMove[]` | Move history in play order |
| `canUndo` | `boolean` | Drives the Undo button's enabled state |
| `undoDepth` | `number` | How many moves the next undo removes (1, or 2 in computer mode) |
| `scoreboard` | `Scoreboard` | Embedded so the client needs a single round trip |

### GameMove

| Field | Type | Example |
| --- | --- | --- |
| `moveNumber` | `number` | `1` |
| `player` | `"X" \| "O"` | `"X"` |
| `row` | `number` | `0` |
| `column` | `number` | `0` |
| `cellIndex` | `number` | `0` |
| `position` | `string` | `"Row 1, Column 1"` |

### Scoreboard

```json
{ "xWins": 0, "oWins": 0, "draws": 0 }
```

### Problem details

Every rejection uses RFC 7807 with an added `errorCode`:

```json
{
  "title": "Invalid game operation",
  "status": 409,
  "detail": "That cell is already taken.",
  "instance": "/api/games/c6a9f958-.../moves",
  "errorCode": "CellOccupied"
}
```

---

## POST /api/games

Creates a session with an empty board and X to play.

**Request**

```json
{ "mode": "TwoPlayer" }
```

`mode` is optional and defaults to `"TwoPlayer"`. An empty body is accepted.

**Responses**

| Status | Body |
| --- | --- |
| `201 Created` | `GameState`, with a `Location` header pointing at `GET /api/games/{id}` |

```bash
curl -X POST http://localhost:5090/api/games \
  -H 'Content-Type: application/json' \
  -d '{"mode":"Computer"}'
```

---

## GET /api/games/{id}

Reads the current state. Never mutates anything — in particular, reading a finished game does
not re-score it.

| Status | Body |
| --- | --- |
| `200 OK` | `GameState` |
| `404 Not Found` | Problem details, `errorCode: "GameNotFound"` |

---

## POST /api/games/{id}/moves

Submits a move. In computer mode the engine's reply is applied in the same request, so the
response already contains both moves and it is X's turn again.

**Request** — the player is required; the cell may be given either way:

```json
{ "player": "X", "cellIndex": 4 }
```

```json
{ "player": "X", "row": 1, "column": 1 }
```

`cellIndex` wins if both forms are present. `row` and `column` are range-checked
independently, so `{"row": 0, "column": 3}` is rejected as off-board rather than being folded
into cell 3.

**Responses**

| Status | `errorCode` | When |
| --- | --- | --- |
| `200 OK` | — | Move applied; body is the new `GameState` |
| `400 Bad Request` | `InvalidMoveRequest` | Neither `cellIndex` nor both `row` and `column` supplied, or `player` missing |
| `400 Bad Request` | `OutOfBoard` | Cell index outside 0–8, or row/column outside 0–2 |
| `404 Not Found` | `GameNotFound` | Unknown session |
| `409 Conflict` | `CellOccupied` | The cell already holds a mark |
| `409 Conflict` | `GameCompleted` | The game has already been won or drawn |
| `409 Conflict` | `WrongPlayer` | It is not that player's turn |
| `409 Conflict` | `NotHumanControlled` | A client tried to play O in computer mode |

A rejected move changes nothing — in particular the current player is unchanged.

```bash
curl -X POST http://localhost:5090/api/games/$ID/moves \
  -H 'Content-Type: application/json' \
  -d '{"player":"X","row":0,"column":0}'
```

---

## POST /api/games/{id}/undo

Takes back the most recent move, or in computer mode the computer's reply together with the
human move before it. Available after a completed game (Option B); reversing a completed game
also removes its result from the scoreboard.

Body is ignored; send `{}` or nothing.

| Status | `errorCode` | When |
| --- | --- | --- |
| `200 OK` | — | Body is the restored `GameState` |
| `404 Not Found` | `GameNotFound` | Unknown session |
| `409 Conflict` | `NothingToUndo` | No moves left to undo |

**Undo depth by mode**

| Mode | Last move | Removed |
| --- | --- | --- |
| Two Player | either | 1 |
| Computer | O (the computer's reply) | 2 — the pair, returning the turn to X |
| Computer | X | 1 — which only happens when X's move ended the game, so there was no reply |

---

## POST /api/games/{id}/reset

Starts a fresh game in the same session: board cleared, history cleared, status cleared, X to
play. **The scoreboard is left unchanged.** The session keeps its id.

**Request** — optional; supplying a mode switches the session as it resets:

```json
{ "mode": "Computer" }
```

| Status | Body |
| --- | --- |
| `200 OK` | The cleared `GameState` |
| `404 Not Found` | Problem details, `errorCode: "GameNotFound"` |

---

## GET /api/scoreboard

| Status | Body |
| --- | --- |
| `200 OK` | `Scoreboard` |

The scoreboard is global to the API instance and shared by every game session.

---

## POST /api/scoreboard/reset

Zeroes the tallies. Games in progress are unaffected; only the counts are cleared.

| Status | Body |
| --- | --- |
| `200 OK` | `Scoreboard`, all zeros |

---

## Scoreboard bookkeeping

Each game stores which result it has already contributed (`XWin`, `OWin`, `Draw`, or none).
After every mutation the API compares the game's current result with its recorded one:

- unchanged → nothing happens (so a completed game is never counted twice);
- newly completed → the result is applied once and recorded;
- reversed by undo → the recorded result is taken back off and the record cleared.

Tallies are clamped at zero, so no sequence of operations can produce a negative count.

---

## A worked session

```bash
BASE=http://localhost:5090/api

# 1. New game against the computer
ID=$(curl -s -X POST $BASE/games -H 'Content-Type: application/json' \
     -d '{"mode":"Computer"}' | jq -r .id)

# 2. X takes the top-left corner; the computer answers in the same response
curl -s -X POST $BASE/games/$ID/moves -H 'Content-Type: application/json' \
     -d '{"player":"X","row":0,"column":0}' | jq '{board, currentPlayer, undoDepth}'
# => board has X at 0 and O at 4 (the computer took the centre), currentPlayer "X", undoDepth 2

# 3. Undo removes both moves and returns the turn to X
curl -s -X POST $BASE/games/$ID/undo | jq '{moves, currentPlayer}'
# => moves [], currentPlayer "X"

# 4. Read the scoreboard, then clear it
curl -s $BASE/scoreboard
curl -s -X POST $BASE/scoreboard/reset
```
