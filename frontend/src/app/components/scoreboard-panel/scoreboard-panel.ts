import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Scoreboard } from '../../models/game.models';

/**
 * Session scoreboard display. The numbers are served by the backend and survive Reset Game;
 * only the explicit Reset Scoreboard action clears them, and that button lives in the shell's
 * action row beside Reset Game and Undo Last Move so all three required controls sit together.
 */
@Component({
  selector: 'app-scoreboard-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="panel">
      <h2>Scoreboard</h2>

      <dl class="tallies">
        <div class="tally">
          <dt class="mark mark--x">X wins</dt>
          <dd data-testid="x-wins">{{ scoreboard().xWins }}</dd>
        </div>
        <div class="tally">
          <dt class="mark mark--o">O wins</dt>
          <dd data-testid="o-wins">{{ scoreboard().oWins }}</dd>
        </div>
        <div class="tally">
          <dt>Draws</dt>
          <dd data-testid="draws">{{ scoreboard().draws }}</dd>
        </div>
      </dl>
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

    .tallies {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 0.5rem;
      margin: 0;
    }

    .tally {
      text-align: center;
      padding: 0.5rem 0.25rem;
      border: 1px solid var(--line);
      border-radius: 0.5rem;
    }

    dt {
      font-size: 0.75rem;
      font-weight: 600;
      color: var(--ink-muted);
    }

    dd {
      margin: 0.15rem 0 0;
      font-size: 1.5rem;
      font-weight: 700;
      font-variant-numeric: tabular-nums;
    }

    .mark--x {
      color: var(--mark-x);
    }

    .mark--o {
      color: var(--mark-o);
    }
  `,
})
export class ScoreboardPanelComponent {
  /** Current tallies from the backend. */
  readonly scoreboard = input.required<Scoreboard>();
}
