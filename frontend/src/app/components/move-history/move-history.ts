import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { GameMove } from '../../models/game.models';

/**
 * Move history for the current game: number, player and cell position, in the layout the
 * specification's example uses. The position string is formatted by the backend so the table
 * and the API agree on wording.
 */
@Component({
  selector: 'app-move-history',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="panel">
      <h2>Move history</h2>

      @if (moves().length === 0) {
        <p class="empty">No moves yet.</p>
      } @else {
        <div class="scroll">
          <table>
            <thead>
              <tr>
                <th scope="col">Move</th>
                <th scope="col">Player</th>
                <th scope="col">Position</th>
              </tr>
            </thead>
            <tbody>
              @for (move of moves(); track move.moveNumber) {
                <tr>
                  <td>{{ move.moveNumber }}</td>
                  <td class="mark" [class.mark--x]="move.player === 'X'" [class.mark--o]="move.player === 'O'">
                    {{ move.player }}
                  </td>
                  <td>{{ move.position }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
  `,
  styles: `
    .panel {
      background: var(--surface);
      border: 1px solid var(--line);
      border-radius: 0.75rem;
      padding: 1rem 1.25rem;
    }

    h2 {
      margin: 0 0 0.75rem;
      font-size: 0.8rem;
      font-weight: 700;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      color: var(--ink-muted);
    }

    .empty {
      margin: 0;
      color: var(--ink-muted);
      font-size: 0.9rem;
    }

    .scroll {
      max-height: 16rem;
      overflow-y: auto;
    }

    table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.9rem;
    }

    th,
    td {
      text-align: left;
      padding: 0.35rem 0.5rem 0.35rem 0;
      border-bottom: 1px solid var(--line);
    }

    th {
      position: sticky;
      top: 0;
      background: var(--surface);
      color: var(--ink-muted);
      font-weight: 600;
    }

    tbody tr:last-child td {
      border-bottom: none;
    }

    .mark {
      font-weight: 700;
    }

    .mark--x {
      color: var(--mark-x);
    }

    .mark--o {
      color: var(--mark-o);
    }
  `,
})
export class MoveHistoryComponent {
  /** Moves for the current game, in play order. */
  readonly moves = input.required<readonly GameMove[]>();
}
