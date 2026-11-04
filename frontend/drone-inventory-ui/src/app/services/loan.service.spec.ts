import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { Loan, LoanHistoryEntry } from '../models/loan.model';
import { LoanService } from './loan.service';

describe('LoanService', () => {
  let service: LoanService;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiBaseUrl}/loans`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(LoanService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should list active loans from GET /api/loans/active', () => {
    const loans: Loan[] = [
      {
        id: 1,
        studentGroupName: 'Group 3',
        droneName: 'TELLO-07',
        batteries: 4,
        propellers: 8,
        loanDate: '2026-08-19'
      }
    ];
    let result: Loan[] | undefined;

    service.getActive().subscribe((response) => (result = response));

    const request = httpMock.expectOne(`${baseUrl}/active`);
    expect(request.request.method).toBe('GET');
    request.flush(loans);
    expect(result).toEqual(loans);
  });

  it('should list returned loans from GET /api/loans/history', () => {
    const history: LoanHistoryEntry[] = [
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
    let result: LoanHistoryEntry[] | undefined;

    service.getHistory().subscribe((response) => (result = response));

    const request = httpMock.expectOne(`${baseUrl}/history`);
    expect(request.request.method).toBe('GET');
    request.flush(history);
    expect(result).toEqual(history);
  });

  it('should post a new loan to /api/loans', () => {
    const created: Loan = {
      id: 1,
      studentGroupName: 'Group 3',
      droneName: 'TELLO-07',
      batteries: 4,
      propellers: 8,
      loanDate: '2026-08-19'
    };
    const body = { droneId: 2, studentGroupId: 1, batteries: 4, propellers: 8 };
    let result: Loan | undefined;

    service.create(body).subscribe((response) => (result = response));

    const request = httpMock.expectOne(baseUrl);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush(created);
    expect(result).toEqual(created);
  });

  it('should post the return of a loan to /api/loans/{id}/return', () => {
    const returned: Loan = {
      id: 1,
      studentGroupName: 'Group 3',
      droneName: 'TELLO-07',
      batteries: 4,
      propellers: 8,
      loanDate: '2026-08-19'
    };
    let result: Loan | undefined;

    service.returnLoan(1).subscribe((response) => (result = response));

    const request = httpMock.expectOne(`${baseUrl}/1/return`);
    expect(request.request.method).toBe('POST');
    request.flush(returned);
    expect(result).toEqual(returned);
  });
});
