import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../core/api-config';
import { gameState } from '../testing/game-state.fixture';
import { GameApiService } from './game-api.service';

const BASE = 'http://test-api/api';

describe('GameApiService', () => {
  let service: GameApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: BASE },
      ],
    });

    service = TestBed.inject(GameApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('posts the mode when creating a game', () => {
    service.createGame('Computer').subscribe();

    const request = http.expectOne(`${BASE}/games`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ mode: 'Computer' });
    request.flush(gameState({ mode: 'Computer' }));
  });

  it('reads a game by id', () => {
    service.getGame('abc').subscribe();

    const request = http.expectOne(`${BASE}/games/abc`);
    expect(request.request.method).toBe('GET');
    request.flush(gameState());
  });

  it('sends the player and the cell index with a move', () => {
    service.submitMove('abc', 'X', 4).subscribe();

    const request = http.expectOne(`${BASE}/games/abc/moves`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ player: 'X', cellIndex: 4 });
    request.flush(gameState());
  });

  it('posts to the undo endpoint', () => {
    service.undoLastMove('abc').subscribe();

    const request = http.expectOne(`${BASE}/games/abc/undo`);
    expect(request.request.method).toBe('POST');
    request.flush(gameState());
  });

  it('omits the mode when resetting without one', () => {
    service.resetGame('abc').subscribe();

    const request = http.expectOne(`${BASE}/games/abc/reset`);
    expect(request.request.body).toEqual({});
    request.flush(gameState());
  });

  it('includes the mode when resetting into a different one', () => {
    service.resetGame('abc', 'Computer').subscribe();

    const request = http.expectOne(`${BASE}/games/abc/reset`);
    expect(request.request.body).toEqual({ mode: 'Computer' });
    request.flush(gameState({ mode: 'Computer' }));
  });

  it('reads and resets the scoreboard', () => {
    service.getScoreboard().subscribe();
    const read = http.expectOne(`${BASE}/scoreboard`);
    expect(read.request.method).toBe('GET');
    read.flush({ xWins: 1, oWins: 0, draws: 0 });

    service.resetScoreboard().subscribe();
    const reset = http.expectOne(`${BASE}/scoreboard/reset`);
    expect(reset.request.method).toBe('POST');
    reset.flush({ xWins: 0, oWins: 0, draws: 0 });
  });
});
