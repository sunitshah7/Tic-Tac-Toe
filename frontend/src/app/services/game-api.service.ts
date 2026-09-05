import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../core/api-config';
import { GameMode, GameState, PlayerMark, Scoreboard } from '../models/game.models';

/**
 * Thin transport layer over the REST API. It holds no game state of its own: the backend is
 * the source of truth, so every method simply returns what the server says the state now is.
 */
@Injectable({ providedIn: 'root' })
export class GameApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  /** POST /api/games - starts a new session in the given mode. */
  createGame(mode: GameMode): Observable<GameState> {
    return this.http.post<GameState>(`${this.baseUrl}/games`, { mode });
  }

  /** GET /api/games/{id} - reads the current state. */
  getGame(id: string): Observable<GameState> {
    return this.http.get<GameState>(`${this.baseUrl}/games/${id}`);
  }

  /**
   * POST /api/games/{id}/moves - submits a move. In computer mode the response already
   * includes the engine's reply.
   */
  submitMove(id: string, player: PlayerMark, cellIndex: number): Observable<GameState> {
    return this.http.post<GameState>(`${this.baseUrl}/games/${id}/moves`, { player, cellIndex });
  }

  /** POST /api/games/{id}/undo - takes back the last move, or move pair in computer mode. */
  undoLastMove(id: string): Observable<GameState> {
    return this.http.post<GameState>(`${this.baseUrl}/games/${id}/undo`, {});
  }

  /** POST /api/games/{id}/reset - clears the board and history, leaving the scoreboard alone. */
  resetGame(id: string, mode?: GameMode): Observable<GameState> {
    return this.http.post<GameState>(`${this.baseUrl}/games/${id}/reset`, mode ? { mode } : {});
  }

  /** GET /api/scoreboard - reads the session tallies. */
  getScoreboard(): Observable<Scoreboard> {
    return this.http.get<Scoreboard>(`${this.baseUrl}/scoreboard`);
  }

  /** POST /api/scoreboard/reset - zeroes the tallies. */
  resetScoreboard(): Observable<Scoreboard> {
    return this.http.post<Scoreboard>(`${this.baseUrl}/scoreboard/reset`, {});
  }
}
