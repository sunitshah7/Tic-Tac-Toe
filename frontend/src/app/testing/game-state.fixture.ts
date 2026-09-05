import { GameState, PlayerMark } from '../models/game.models';

/** A blank board, as the backend sends it for a new game. */
export const emptyBoard: ReadonlyArray<PlayerMark | null> = Array<PlayerMark | null>(9).fill(null);

/**
 * Builds a game-state response for tests. Defaults describe a freshly created two-player
 * game; each test overrides only the fields it cares about.
 */
export function gameState(overrides: Partial<GameState> = {}): GameState {
  return {
    id: '11111111-2222-3333-4444-555555555555',
    mode: 'TwoPlayer',
    board: emptyBoard,
    currentPlayer: 'X',
    status: 'InProgress',
    winner: null,
    winningCells: [],
    moves: [],
    canUndo: false,
    undoDepth: 0,
    scoreboard: { xWins: 0, oWins: 0, draws: 0 },
    ...overrides,
  };
}

/** A board with the given marks placed, everything else empty. */
export function boardWith(marks: Record<number, PlayerMark>): ReadonlyArray<PlayerMark | null> {
  return emptyBoard.map((_, index) => marks[index] ?? null);
}
