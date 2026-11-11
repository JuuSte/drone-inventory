import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { LoanHistoryEntry } from '../../models/loan.model';
import { HistoryPage } from './history';

describe('HistoryPage', () => {
  let fixture: ComponentFixture<HistoryPage>;
  let httpMock: HttpTestingController;
  const url = `${environment.apiBaseUrl}/loans/history`;

  const returned: LoanHistoryEntry[] = [
    {
      id: 1,
      studentGroupName: 'Group 3',
      droneName: 'TELLO-07',
      batteries: 4,
      propellers: 8,
      loanDate: '2026-08-19',
      returnDate: '2026-08-20'
    }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HistoryPage],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    fixture = TestBed.createComponent(HistoryPage);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  function element(selector: string): HTMLElement {
    return (fixture.nativeElement as HTMLElement).querySelector(selector) as HTMLElement;
  }

  function rows(): (string | undefined)[][] {
    return Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('.history-list tbody tr')
    ).map((row) => Array.from(row.querySelectorAll('td')).map((cell) => cell.textContent?.trim()));
  }

  it('should list a returned Loan with LoanDate, ReturnDate and accessories', () => {
    httpMock.expectOne(url).flush(returned);
    fixture.detectChanges();

    expect(rows()).toEqual([['Group 3', 'TELLO-07', '4', '8', '19.08.2026', '20.08.2026', 'Returned']]);
  });

  it('should show both Loans after the same Drone was loaned and returned twice', () => {
    httpMock.expectOne(url).flush([
      ...returned,
      { ...returned[0], id: 2, loanDate: '2026-08-21', returnDate: '2026-08-22' }
    ]);
    fixture.detectChanges();

    expect(rows()).toEqual([
      ['Group 3', 'TELLO-07', '4', '8', '19.08.2026', '20.08.2026', 'Returned'],
      ['Group 3', 'TELLO-07', '4', '8', '21.08.2026', '22.08.2026', 'Returned']
    ]);
  });

  it('should show an empty state when no Loan was returned yet', () => {
    httpMock.expectOne(url).flush([]);
    fixture.detectChanges();

    expect(rows()).toEqual([['No returned loans yet.']]);
  });

  it('should show the backend error when the history cannot be loaded', () => {
    httpMock.expectOne(url).flush(
      { title: 'Internal Server Error', detail: 'History is unavailable.', status: 500 },
      { status: 500, statusText: 'Internal Server Error' }
    );
    fixture.detectChanges();

    expect(element('[role="alert"]').textContent?.trim()).toBe('History is unavailable.');
  });

  // web-ui scenarios (UI-001, UI-002, UI-003)

  // GIVEN a returned Loan exists
  // WHEN the History page is shown
  // THEN the returned Loan shows the badge "Returned"
  it('Loan status badges (Returned)', () => {
    httpMock.expectOne(url).flush(returned);
    fixture.detectChanges();

    const badge = element('.history-list tbody .badge');
    expect(badge.textContent?.trim()).toBe('Returned');
    expect(badge.classList).toContain('badge--success');
  });

  // WHEN the History page is opened
  // THEN the page shows a single main heading with the page name
  it('Every page has a title', () => {
    httpMock.expectOne(url).flush([]);
    fixture.detectChanges();

    const headings = (fixture.nativeElement as HTMLElement).querySelectorAll('h1');
    expect(headings.length).toBe(1);
    expect(headings[0].textContent?.trim()).toBe('History');
  });

  // Quantities right-aligned, table scrolls inside its card (UI-003, UI-007).
  it('right-aligns the quantities and wraps the table for horizontal scrolling', () => {
    httpMock.expectOne(url).flush(returned);
    fixture.detectChanges();

    const cells = Array.from(element('.history-list tbody tr').querySelectorAll('td'));
    const headers = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('.history-list th'));
    for (const index of [2, 3]) {
      expect(cells[index].classList).toContain('num');
      expect(headers[index].classList).toContain('num');
    }
    expect(element('.history-list').closest('.table-wrap')).not.toBeNull();
  });
});
