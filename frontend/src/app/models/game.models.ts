/**
 * Wire contract shared with the .NET backend. The API serialises its enums as names, so the
 * string unions here line up one-to-one with the C# types and the compiler catches any drift
 * at the point where a response is consumed.
 */

/** A mark on the board. */
export type PlayerMark = 'X' | 'O';

/** Which side controls O. */
export type GameMode = 'TwoPlayer' | 'Computer';

/** Lifecycle of a single game. */
export type GameStatus = 'InProgress' | 'Won' | 'Draw';

/** One row of the move history. */
export interface GameMove {
  readonly moveNumber: number;
  readonly player: PlayerMark;
  readonly row: number;
  readonly column: number;
  readonly cellIndex: number;
  /** Display text formatted by the backend, e.g. "Row 1, Column 1". */
  readonly position: string;
}

/** Session-level tallies. */
export interface Scoreboard {
  readonly xWins: number;
  readonly oWins: number;
  readonly draws: number;
}

/** Everything needed to render the game; the backend returns this from every game action. */
export interface GameState {
  readonly id: string;
  readonly mode: GameMode;
  /** Nine cells, row-major; null where empty. */
  readonly board: ReadonlyArray<PlayerMark | null>;
  readonly currentPlayer: PlayerMark | null;
  readonly status: GameStatus;
  readonly winner: PlayerMark | null;
  readonly winningCells: readonly number[];
  readonly moves: readonly GameMove[];
  readonly canUndo: boolean;
  /** How many moves the next undo removes: 1 in two-player mode, usually 2 against the computer. */
  readonly undoDepth: number;
  readonly scoreboard: Scoreboard;
}

/** RFC 7807 body the API returns for a rejected operation. */
export interface ApiProblem {
  readonly title?: string;
  readonly detail?: string;
  readonly status?: number;
  /** Stable machine-readable reason, e.g. "CellOccupied". */
  readonly errorCode?: string;
}
