import { ComponentFixture, TestBed } from '@angular/core/testing';
import { boardWith, emptyBoard } from '../../testing/game-state.fixture';
import { GameBoardComponent } from './game-board';

describe('GameBoardComponent', () => {
  let fixture: ComponentFixture<GameBoardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [GameBoardComponent] }).compileComponents();
    fixture = TestBed.createComponent(GameBoardComponent);
  });

  function cells(): HTMLButtonElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('button.cell'));
  }

  it('renders nine cells', async () => {
    fixture.componentRef.setInput('board', emptyBoard);
    await fixture.whenStable();

    expect(cells().length).toBe(9);
  });

  it('shows the marks the backend placed', async () => {
    fixture.componentRef.setInput('board', boardWith({ 0: 'X', 4: 'O' }));
    fixture.componentRef.setInput('active', true);
    await fixture.whenStable();

    expect(cells()[0].textContent?.trim()).toBe('X');
    expect(cells()[4].textContent?.trim()).toBe('O');
    expect(cells()[8].textContent?.trim()).toBe('');
  });

  it('locks a cell once it holds a mark', async () => {
    fixture.componentRef.setInput('board', boardWith({ 0: 'X' }));
    fixture.componentRef.setInput('active', true);
    await fixture.whenStable();

    expect(cells()[0].disabled).toBe(true);
    expect(cells()[1].disabled).toBe(false);
  });

  it('highlights the winning cells', async () => {
    fixture.componentRef.setInput('board', boardWith({ 0: 'X', 1: 'X', 2: 'X' }));
    fixture.componentRef.setInput('winningCells', [0, 1, 2]);
    await fixture.whenStable();

    expect(cells()[0].classList.contains('cell--winning')).toBe(true);
    expect(cells()[3].classList.contains('cell--winning')).toBe(false);
  });

  it('emits the index of a clicked empty cell', async () => {
    const selected: number[] = [];
    fixture.componentRef.setInput('board', emptyBoard);
    fixture.componentRef.setInput('active', true);
    fixture.componentInstance.cellSelected.subscribe((index) => selected.push(index));
    await fixture.whenStable();

    cells()[5].click();

    expect(selected).toEqual([5]);
  });

  it('emits nothing while the board is inactive', async () => {
    const selected: number[] = [];
    fixture.componentRef.setInput('board', emptyBoard);
    fixture.componentRef.setInput('active', false);
    fixture.componentInstance.cellSelected.subscribe((index) => selected.push(index));
    await fixture.whenStable();

    cells()[5].click();

    expect(selected).toEqual([]);
    expect(cells()[5].disabled).toBe(true);
  });

  it('labels every cell for assistive technology', async () => {
    fixture.componentRef.setInput('board', boardWith({ 0: 'X' }));
    await fixture.whenStable();

    expect(cells()[0].getAttribute('aria-label')).toBe('Row 1, Column 1, X');
    expect(cells()[8].getAttribute('aria-label')).toBe('Row 3, Column 3, empty');
  });
});
