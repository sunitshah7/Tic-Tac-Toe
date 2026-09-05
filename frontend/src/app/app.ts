import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { GameBoardComponent } from './components/game-board/game-board';
import { MoveHistoryComponent } from './components/move-history/move-history';
import { ScoreboardPanelComponent } from './components/scoreboard-panel/scoreboard-panel';
import { GameMode } from './models/game.models';
import { GameStore } from './services/game-store';

/**
 * Page shell. It wires the presentational components to the store and owns no game logic of
 * its own; every button hands straight through to a backend call.
 */
@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [GameBoardComponent, MoveHistoryComponent, ScoreboardPanelComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  protected readonly store = inject(GameStore);

  /** The two modes, in the order the specification lists them. */
  protected readonly modes: ReadonlyArray<{ value: GameMode; label: string }> = [
    { value: 'TwoPlayer', label: 'Two Player' },
    { value: 'Computer', label: 'Play Against Computer' },
  ];

  ngOnInit(): void {
    void this.store.startNewGame('TwoPlayer');
  }

  protected onCellSelected(cellIndex: number): void {
    void this.store.play(cellIndex);
  }

  protected onModeChange(mode: GameMode): void {
    void this.store.changeMode(mode);
  }

  protected onUndo(): void {
    void this.store.undo();
  }

  protected onResetGame(): void {
    void this.store.resetGame();
  }

  protected onResetScoreboard(): void {
    void this.store.resetScoreboard();
  }
}
