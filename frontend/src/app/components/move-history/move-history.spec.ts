import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GameMove } from '../../models/game.models';
import { MoveHistoryComponent } from './move-history';

const moves: GameMove[] = [
  { moveNumber: 1, player: 'X', row: 0, column: 0, cellIndex: 0, position: 'Row 1, Column 1' },
  { moveNumber: 2, player: 'O', row: 1, column: 1, cellIndex: 4, position: 'Row 2, Column 2' },
];

describe('MoveHistoryComponent', () => {
  let fixture: ComponentFixture<MoveHistoryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [MoveHistoryComponent] }).compileComponents();
    fixture = TestBed.createComponent(MoveHistoryComponent);
  });

  it('says so when no moves have been played', async () => {
    fixture.componentRef.setInput('moves', []);
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('No moves yet.');
    expect(fixture.nativeElement.querySelector('table')).toBeNull();
  });

  it('renders one row per move with number, player and position', async () => {
    fixture.componentRef.setInput('moves', moves);
    await fixture.whenStable();

    const rows: HTMLTableRowElement[] = Array.from(
      fixture.nativeElement.querySelectorAll('tbody tr'),
    );
    expect(rows.length).toBe(2);

    const first = Array.from(rows[0].cells).map((cell) => cell.textContent?.trim());
    expect(first).toEqual(['1', 'X', 'Row 1, Column 1']);

    const second = Array.from(rows[1].cells).map((cell) => cell.textContent?.trim());
    expect(second).toEqual(['2', 'O', 'Row 2, Column 2']);
  });
});
