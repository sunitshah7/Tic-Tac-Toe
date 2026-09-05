import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ScoreboardPanelComponent } from './scoreboard-panel';

describe('ScoreboardPanelComponent', () => {
  let fixture: ComponentFixture<ScoreboardPanelComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ScoreboardPanelComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ScoreboardPanelComponent);
    fixture.componentRef.setInput('scoreboard', { xWins: 3, oWins: 2, draws: 1 });
  });

  function text(testId: string): string {
    return fixture.nativeElement.querySelector(`[data-testid="${testId}"]`).textContent.trim();
  }

  it('shows the three tallies', async () => {
    await fixture.whenStable();

    expect(text('x-wins')).toBe('3');
    expect(text('o-wins')).toBe('2');
    expect(text('draws')).toBe('1');
  });

  it('updates when the backend reports new tallies', async () => {
    await fixture.whenStable();

    fixture.componentRef.setInput('scoreboard', { xWins: 4, oWins: 2, draws: 1 });
    await fixture.whenStable();

    expect(text('x-wins')).toBe('4');
  });

  it('is display only - Reset Scoreboard lives in the shell action row', async () => {
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelectorAll('button').length).toBe(0);
  });
});
