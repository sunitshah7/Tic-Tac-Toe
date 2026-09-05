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

  it('emits when reset is clicked', async () => {
    let requested = 0;
    fixture.componentInstance.resetRequested.subscribe(() => requested++);
    await fixture.whenStable();

    fixture.nativeElement.querySelector('button.link').click();

    expect(requested).toBe(1);
  });

  it('disables reset while a request is in flight', async () => {
    fixture.componentRef.setInput('disabled', true);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('button.link').disabled).toBe(true);
  });
});
