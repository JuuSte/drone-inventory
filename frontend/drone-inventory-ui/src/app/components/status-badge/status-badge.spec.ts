import { TestBed } from '@angular/core/testing';
import { Status, StatusBadge } from './status-badge';

describe('StatusBadge', () => {
  const cases: ReadonlyArray<readonly [Status, 'success' | 'warning']> = [
    ['Available', 'success'],
    ['Returned', 'success'],
    ['Loaned', 'warning'],
    ['Active', 'warning']
  ];

  it.each(cases)('shows %s with the %s style', (status, tone) => {
    const fixture = TestBed.createComponent(StatusBadge);
    fixture.componentRef.setInput('status', status);
    fixture.detectChanges();

    const badge = (fixture.nativeElement as HTMLElement).querySelector('.badge');

    expect(badge).toBeTruthy();
    expect(badge?.textContent?.trim()).toBe(status);
    expect(badge?.classList).toContain(`badge--${tone}`);
  });

  it('uses exactly one badge style per status', () => {
    const fixture = TestBed.createComponent(StatusBadge);
    fixture.componentRef.setInput('status', 'Loaned');
    fixture.detectChanges();

    const badge = (fixture.nativeElement as HTMLElement).querySelector('.badge')!;

    expect(badge.classList).toContain('badge--warning');
    expect(badge.classList).not.toContain('badge--success');
  });
});
