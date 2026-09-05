import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { App } from './app';
import { API_BASE_URL } from './core/api-config';
import { GameState } from './models/game.models';
import { boardWith, gameState } from './testing/game-state.fixture';

const BASE = 'http://test-api/api';

describe('App', () => {
  let fixture: ComponentFixture<App>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: BASE },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(App);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  /** Renders the shell and answers the game it creates on startup. */
  async function render(state: GameState = gameState()): Promise<void> {
    fixture.detectChanges();
    http.expectOne(`${BASE}/games`).flush(state);
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function query(selector: string): HTMLElement | null {
    return fixture.nativeElement.querySelector(selector);
  }

  it('creates a two-player game on startup and shows whose turn it is', async () => {
    await render();

    expect(query('[data-testid="status"]')?.textContent?.trim()).toBe('Player X to move');
    expect(fixture.nativeElement.querySelectorAll('button.cell').length).toBe(9);
  });

  it('shows the selected mode', async () => {
    await render(gameState({ mode: 'Computer' }));

    const active = fixture.nativeElement.querySelector('button.mode--active');
    expect(active.textContent.trim()).toBe('Play Against Computer');
  });

  it('sends a move when a cell is clicked', async () => {
    await render();

    const cells: HTMLButtonElement[] = Array.from(
      fixture.nativeElement.querySelectorAll('button.cell'),
    );
    cells[4].click();

    const request = http.expectOne(`${BASE}/games/${gameState().id}/moves`);
    expect(request.request.body).toEqual({ player: 'X', cellIndex: 4 });
    request.flush(
      gameState({ board: boardWith({ 4: 'X' }), currentPlayer: 'O', canUndo: true, undoDepth: 1 }),
    );
    await fixture.whenStable();
    fixture.detectChanges();

    expect(query('[data-testid="status"]')?.textContent?.trim()).toBe('Player O to move');
  });

  it('disables undo until there is something to undo', async () => {
    await render();

    const undo = query('[data-testid="undo"]') as HTMLButtonElement;
    expect(undo.disabled).toBe(true);
  });

  it('enables undo once a move has been played', async () => {
    await render(gameState({ canUndo: true, undoDepth: 1 }));

    const undo = query('[data-testid="undo"]') as HTMLButtonElement;
    expect(undo.disabled).toBe(false);
  });

  it('announces a win and highlights the winning line', async () => {
    await render(
      gameState({
        status: 'Won',
        winner: 'X',
        currentPlayer: null,
        winningCells: [0, 1, 2],
        board: boardWith({ 0: 'X', 1: 'X', 2: 'X', 3: 'O', 4: 'O' }),
        scoreboard: { xWins: 1, oWins: 0, draws: 0 },
      }),
    );

    expect(query('[data-testid="status"]')?.textContent?.trim()).toBe('Player X wins!');
    expect(fixture.nativeElement.querySelectorAll('.cell--winning').length).toBe(3);
    expect(query('[data-testid="x-wins"]')?.textContent?.trim()).toBe('1');
  });

  it('announces a draw', async () => {
    await render(gameState({ status: 'Draw', currentPlayer: null }));

    expect(query('[data-testid="status"]')?.textContent?.trim()).toBe("It's a draw.");
  });

  it('shows the error the backend returned', async () => {
    await render();

    const cells: HTMLButtonElement[] = Array.from(
      fixture.nativeElement.querySelectorAll('button.cell'),
    );
    cells[0].click();

    http
      .expectOne(`${BASE}/games/${gameState().id}/moves`)
      .flush(
        { detail: 'That cell is already taken.', errorCode: 'CellOccupied' },
        { status: 409, statusText: 'Conflict' },
      );
    await fixture.whenStable();
    fixture.detectChanges();

    expect(query('.alert')?.textContent).toContain('That cell is already taken.');
  });
});
