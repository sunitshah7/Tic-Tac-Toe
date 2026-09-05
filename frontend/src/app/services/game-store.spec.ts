import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../core/api-config';
import { boardWith, gameState } from '../testing/game-state.fixture';
import { GameStore } from './game-store';

const BASE = 'http://test-api/api';

describe('GameStore', () => {
  let store: GameStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: BASE },
      ],
    });

    store = TestBed.inject(GameStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  /** Creates a game and returns the state the backend "sent". */
  async function start(overrides = {}) {
    const state = gameState(overrides);
    const pending = store.startNewGame(state.mode);
    http.expectOne(`${BASE}/games`).flush(state);
    await pending;
    return state;
  }

  it('adopts the state the backend returns', async () => {
    const state = await start();

    expect(store.state()).toEqual(state);
    expect(store.statusMessage()).toBe('Player X to move');
    expect(store.isBoardActive()).toBe(true);
  });

  it('sends the mark the backend says is on turn', async () => {
    const state = await start({ currentPlayer: 'O' });

    const pending = store.play(4);
    const request = http.expectOne(`${BASE}/games/${state.id}/moves`);
    expect(request.request.body).toEqual({ player: 'O', cellIndex: 4 });
    request.flush(gameState());
    await pending;
  });

  it('ignores clicks once the game is complete', async () => {
    await start({ status: 'Draw', currentPlayer: null });

    await store.play(0);

    http.expectNone(() => true);
    expect(store.isBoardActive()).toBe(false);
  });

  it('reports the winner and keeps the winning cells for highlighting', async () => {
    await start({
      status: 'Won',
      winner: 'X',
      currentPlayer: null,
      winningCells: [0, 1, 2],
      board: boardWith({ 0: 'X', 1: 'X', 2: 'X', 3: 'O', 4: 'O' }),
      scoreboard: { xWins: 1, oWins: 0, draws: 0 },
    });

    expect(store.statusMessage()).toBe('Player X wins!');
    expect(store.winningCells()).toEqual([0, 1, 2]);
    expect(store.scoreboard().xWins).toBe(1);
    expect(store.isComplete()).toBe(true);
  });

  it('says so when the game is drawn', async () => {
    await start({ status: 'Draw', currentPlayer: null });

    expect(store.statusMessage()).toBe("It's a draw.");
  });

  it('does not call undo when there is nothing to undo', async () => {
    await start({ canUndo: false });

    await store.undo();

    http.expectNone(() => true);
    expect(store.canUndo()).toBe(false);
  });

  it('calls the undo endpoint when moves exist', async () => {
    const state = await start({ canUndo: true });

    const pending = store.undo();
    http.expectOne(`${BASE}/games/${state.id}/undo`).flush(gameState());
    await pending;

    expect(store.moves()).toEqual([]);
  });

  it('resets the game without touching the scoreboard call', async () => {
    const state = await start({ scoreboard: { xWins: 2, oWins: 1, draws: 0 } });

    const pending = store.resetGame();
    const request = http.expectOne(`${BASE}/games/${state.id}/reset`);
    request.flush(gameState({ scoreboard: { xWins: 2, oWins: 1, draws: 0 } }));
    await pending;

    expect(store.scoreboard()).toEqual({ xWins: 2, oWins: 1, draws: 0 });
    expect(store.moves()).toEqual([]);
  });

  it('switches mode by resetting the session', async () => {
    const state = await start();

    const pending = store.changeMode('Computer');
    const request = http.expectOne(`${BASE}/games/${state.id}/reset`);
    expect(request.request.body).toEqual({ mode: 'Computer' });
    request.flush(gameState({ mode: 'Computer' }));
    await pending;

    expect(store.mode()).toBe('Computer');
  });

  it('does nothing when the requested mode is already selected', async () => {
    await start({ mode: 'Computer' });

    await store.changeMode('Computer');

    http.expectNone(() => true);
  });

  it('replaces only the scoreboard when it is reset', async () => {
    await start({
      scoreboard: { xWins: 3, oWins: 1, draws: 2 },
      moves: [{ moveNumber: 1, player: 'X', row: 0, column: 0, cellIndex: 0, position: 'Row 1, Column 1' }],
    });

    const pending = store.resetScoreboard();
    http.expectOne(`${BASE}/scoreboard/reset`).flush({ xWins: 0, oWins: 0, draws: 0 });
    await pending;

    expect(store.scoreboard()).toEqual({ xWins: 0, oWins: 0, draws: 0 });
    expect(store.moves().length).toBe(1);
  });

  it('surfaces the backend explanation for a rejected move', async () => {
    const state = await start();

    const pending = store.play(0);
    http.expectOne(`${BASE}/games/${state.id}/moves`).flush(
      { detail: 'That cell is already taken.', errorCode: 'CellOccupied' },
      { status: 409, statusText: 'Conflict' },
    );
    await pending;

    expect(store.error()).toBe('That cell is already taken.');
    // The last known good state is kept, so the board never contradicts the backend.
    expect(store.state()).toEqual(state);
  });

  it('explains an unreachable backend', async () => {
    const state = await start();

    const pending = store.play(0);
    http
      .expectOne(`${BASE}/games/${state.id}/moves`)
      .error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });
    await pending;

    expect(store.error()).toContain('Cannot reach the backend');
  });

  it('clears the error on the next successful call', async () => {
    const state = await start();

    const failed = store.play(0);
    http
      .expectOne(`${BASE}/games/${state.id}/moves`)
      .flush({ detail: 'nope' }, { status: 409, statusText: 'Conflict' });
    await failed;
    expect(store.error()).toBe('nope');

    const pending = store.play(1);
    http.expectOne(`${BASE}/games/${state.id}/moves`).flush(gameState());
    await pending;

    expect(store.error()).toBeNull();
  });
});
