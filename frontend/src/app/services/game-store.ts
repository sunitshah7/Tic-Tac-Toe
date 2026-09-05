import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  ApiProblem,
  GameMode,
  GameState,
  PlayerMark,
  Scoreboard,
} from '../models/game.models';
import { GameApiService } from './game-api.service';

const EMPTY_BOARD: ReadonlyArray<PlayerMark | null> = Array<PlayerMark | null>(9).fill(null);
const EMPTY_SCOREBOARD: Scoreboard = { xWins: 0, oWins: 0, draws: 0 };

/**
 * Holds the last state the backend reported and exposes it as signals for the components to
 * render. It deliberately never computes a board, a turn or a winner of its own: the backend
 * owns the rules, and mirroring them here is how the two ends drift apart.
 */
@Injectable({ providedIn: 'root' })
export class GameStore {
  private readonly api = inject(GameApiService);

  private readonly gameState = signal<GameState | null>(null);
  private readonly errorMessage = signal<string | null>(null);
  private readonly busy = signal(false);

  /** The raw state from the backend, or null before the first game is created. */
  readonly state = this.gameState.asReadonly();

  /** Message from the most recently rejected request, cleared by the next successful one. */
  readonly error = this.errorMessage.asReadonly();

  /** True while a request is in flight; used to disable the board and buttons. */
  readonly isBusy = this.busy.asReadonly();

  readonly board = computed(() => this.gameState()?.board ?? EMPTY_BOARD);
  readonly moves = computed(() => this.gameState()?.moves ?? []);
  readonly scoreboard = computed(() => this.gameState()?.scoreboard ?? EMPTY_SCOREBOARD);
  readonly mode = computed<GameMode>(() => this.gameState()?.mode ?? 'TwoPlayer');
  readonly winningCells = computed(() => this.gameState()?.winningCells ?? []);
  readonly canUndo = computed(() => (this.gameState()?.canUndo ?? false) && !this.busy());
  readonly isComplete = computed(() => (this.gameState()?.status ?? 'InProgress') !== 'InProgress');

  /** Whether the board should accept clicks at all. */
  readonly isBoardActive = computed(
    () => this.gameState() !== null && !this.isComplete() && !this.busy(),
  );

  /** The line the panel reads: whose turn it is, who won, or that it was a draw. */
  readonly statusMessage = computed(() => {
    const state = this.gameState();
    if (!state) {
      return 'Starting a new game...';
    }

    switch (state.status) {
      case 'Won':
        return `Player ${state.winner} wins!`;
      case 'Draw':
        return "It's a draw.";
      default:
        return `Player ${state.currentPlayer} to move`;
    }
  });

  /** Creates a session. Called once on startup and whenever the mode is switched. */
  async startNewGame(mode: GameMode = this.mode()): Promise<void> {
    await this.run(() => firstValueFrom(this.api.createGame(mode)));
  }

  /**
   * Plays a cell for whoever the backend says is on turn. The mark is sent explicitly so the
   * server can reject a move by the wrong player rather than silently accepting it.
   */
  async play(cellIndex: number): Promise<void> {
    const state = this.gameState();
    if (!state || !state.currentPlayer || this.busy()) {
      return;
    }

    await this.run(() =>
      firstValueFrom(this.api.submitMove(state.id, state.currentPlayer as PlayerMark, cellIndex)),
    );
  }

  /** Takes back the last move, or the move pair in computer mode. */
  async undo(): Promise<void> {
    const state = this.gameState();
    if (!state || !state.canUndo) {
      return;
    }

    await this.run(() => firstValueFrom(this.api.undoLastMove(state.id)));
  }

  /** Clears the board and history for a fresh game, keeping the scoreboard. */
  async resetGame(): Promise<void> {
    const state = this.gameState();
    if (!state) {
      return;
    }

    await this.run(() => firstValueFrom(this.api.resetGame(state.id)));
  }

  /**
   * Switches between two-player and computer mode. The backend resets the session at the same
   * time, because a half-played two-player game has no meaningful reading in computer mode.
   */
  async changeMode(mode: GameMode): Promise<void> {
    const state = this.gameState();
    if (!state) {
      await this.startNewGame(mode);
      return;
    }

    if (state.mode === mode) {
      return;
    }

    await this.run(() => firstValueFrom(this.api.resetGame(state.id, mode)));
  }

  /** Zeroes the tallies without disturbing the game in progress. */
  async resetScoreboard(): Promise<void> {
    this.busy.set(true);
    try {
      const scoreboard = await firstValueFrom(this.api.resetScoreboard());
      const state = this.gameState();
      if (state) {
        this.gameState.set({ ...state, scoreboard });
      }
      this.errorMessage.set(null);
    } catch (error) {
      this.errorMessage.set(describe(error));
    } finally {
      this.busy.set(false);
    }
  }

  /** Discards the current error message. */
  clearError(): void {
    this.errorMessage.set(null);
  }

  /**
   * Runs one backend call, adopting whatever state comes back. A rejection leaves the last
   * known good state on screen and surfaces the server's explanation, so the UI can never
   * show a board the backend disagrees with.
   */
  private async run(operation: () => Promise<GameState>): Promise<void> {
    this.busy.set(true);
    try {
      this.gameState.set(await operation());
      this.errorMessage.set(null);
    } catch (error) {
      this.errorMessage.set(describe(error));
    } finally {
      this.busy.set(false);
    }
  }
}

/** Turns an HTTP failure into the sentence shown to the player. */
function describe(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const problem = error.error as ApiProblem | null;
    if (problem?.detail) {
      return problem.detail;
    }

    if (error.status === 0) {
      return 'Cannot reach the backend. Is the .NET API running on http://localhost:5090?';
    }

    return `The server rejected that request (HTTP ${error.status}).`;
  }

  return 'Something went wrong talking to the backend.';
}
