import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { PlayerMark } from '../../models/game.models';

/**
 * The 3x3 grid. Purely presentational: it renders the board the backend sent and reports
 * clicks upwards. It never decides whether a move is legal - that is the server's job - it
 * only avoids emitting for cells that are visibly unavailable.
 */
@Component({
  selector: 'app-game-board',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="board" role="grid" aria-label="Tic Tac Toe board">
      @for (cell of board(); track $index) {
        <button
          type="button"
          class="cell"
          role="gridcell"
          [class.cell--taken]="cell !== null"
          [class.cell--winning]="winningCells().includes($index)"
          [class.cell--x]="cell === 'X'"
          [class.cell--o]="cell === 'O'"
          [disabled]="!active() || cell !== null"
          [attr.aria-label]="describe($index, cell)"
          (click)="select($index, cell)"
        >
          {{ cell ?? '' }}
        </button>
      }
    </div>
  `,
  styles: `
    .board {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 0.5rem;
      width: min(22rem, 100%);
      aspect-ratio: 1;
    }

    .cell {
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: clamp(2rem, 8vw, 3.25rem);
      font-weight: 700;
      line-height: 1;
      border: 1px solid var(--line);
      border-radius: 0.75rem;
      background: var(--surface);
      color: var(--ink);
      cursor: pointer;
      transition:
        background-color 120ms ease,
        transform 120ms ease;
    }

    .cell:hover:not(:disabled) {
      background: var(--surface-hover);
      transform: translateY(-1px);
    }

    .cell:focus-visible {
      outline: 3px solid var(--accent);
      outline-offset: 2px;
    }

    .cell:disabled {
      cursor: default;
    }

    /* A taken cell is locked for the rest of the game, so it reads as static, not disabled. */
    .cell--taken:disabled {
      opacity: 1;
    }

    .cell--x {
      color: var(--mark-x);
    }

    .cell--o {
      color: var(--mark-o);
    }

    .cell--winning {
      background: var(--win-fill);
      border-color: var(--win-line);
      box-shadow: inset 0 0 0 2px var(--win-line);
    }
  `,
})
export class GameBoardComponent {
  /** Nine cells, row-major, exactly as the backend returned them. */
  readonly board = input.required<ReadonlyArray<PlayerMark | null>>();

  /** Indices to highlight after a win. */
  readonly winningCells = input<readonly number[]>([]);

  /** False while the game is over, a request is in flight, or no game exists yet. */
  readonly active = input(false);

  /** Emits the flat index of a clicked empty cell. */
  readonly cellSelected = output<number>();

  /** Row/column labels for screen readers, 1-based to match the move history. */
  protected readonly labels = computed(() =>
    this.board().map((_, index) => `Row ${Math.floor(index / 3) + 1}, Column ${(index % 3) + 1}`),
  );

  protected describe(index: number, cell: PlayerMark | null): string {
    const position = this.labels()[index];
    return cell === null ? `${position}, empty` : `${position}, ${cell}`;
  }

  protected select(index: number, cell: PlayerMark | null): void {
    if (this.active() && cell === null) {
      this.cellSelected.emit(index);
    }
  }
}
