import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { Drone } from '../../models/drone.model';
import { Loan } from '../../models/loan.model';
import { StudentGroup } from '../../models/student-group.model';
import { LoansPage } from './loans';

describe('LoansPage', () => {
  let fixture: ComponentFixture<LoansPage>;
  let httpMock: HttpTestingController;
  const dronesUrl = `${environment.apiBaseUrl}/drones`;
  const groupsUrl = `${environment.apiBaseUrl}/student-groups`;
  const loansUrl = `${environment.apiBaseUrl}/loans`;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoansPage],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    fixture = TestBed.createComponent(LoansPage);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  function load(drones: Drone[], groups: StudentGroup[], loans: Loan[] = []): void {
    httpMock.expectOne(dronesUrl).flush(drones);
    httpMock.expectOne(groupsUrl).flush(groups);
    httpMock.expectOne(`${loansUrl}/active`).flush(loans);
    fixture.detectChanges();
  }

  function element(selector: string): HTMLElement {
    return (fixture.nativeElement as HTMLElement).querySelector(selector) as HTMLElement;
  }

  function options(selector: string): (string | undefined)[] {
    return Array.from((element(selector) as HTMLSelectElement).options).map((option) =>
      option.textContent?.trim()
    );
  }

  function select(selector: string, index: number): void {
    const selectElement = element(selector) as HTMLSelectElement;
    selectElement.selectedIndex = index;
    selectElement.dispatchEvent(new Event('change'));
    fixture.detectChanges();
  }

  function type(selector: string, value: string): void {
    const input = element(selector) as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function submit(): void {
    element('.loan-form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  function rows(): (string | undefined)[][] {
    return Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('.loan-list tbody tr')).map(
      (row) => Array.from(row.querySelectorAll('td')).map((cell) => cell.textContent?.trim())
    );
  }

  const available: Drone[] = [
    { id: 1, name: 'TELLO-02', description: '', status: 'Loaned' },
    { id: 2, name: 'TELLO-07', description: '', status: 'Available' }
  ];
  const groups: StudentGroup[] = [{ id: 1, name: 'Group 3' }];
  const activeLoan: Loan = {
    id: 1,
    studentGroupName: 'Group 3',
    droneName: 'TELLO-07',
    batteries: 4,
    propellers: 8,
    loanDate: '2026-08-19'
  };

  it('should offer only available drones and the existing StudentGroups', () => {
    load(available, groups);

    expect(options('select[name="droneId"]')).toEqual(['Select a drone', 'TELLO-07']);
    expect(options('select[name="studentGroupId"]')).toEqual(['Select a group', 'Group 3']);
  });

  it('should loan TELLO-07 to Group 3 with 4 batteries and 8 propellers', async () => {
    load(available, groups);

    select('select[name="droneId"]', 1);
    select('select[name="studentGroupId"]', 1);
    type('input[name="batteries"]', '4');
    type('input[name="propellers"]', '8');
    submit();

    const request = httpMock.expectOne(loansUrl);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      droneId: 2,
      studentGroupId: 1,
      batteries: 4,
      propellers: 8
    });
    request.flush(activeLoan);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(element('[role="status"]').textContent?.trim()).toBe('Loan created.');
    expect((element('input[name="batteries"]') as HTMLInputElement).value).toBe('0');
    expect((element('input[name="propellers"]') as HTMLInputElement).value).toBe('0');

    httpMock.expectOne(`${loansUrl}/active`).flush([activeLoan]);
  });

  it('should show the backend error when TELLO-07 is already loaned', () => {
    load(available, groups);

    select('select[name="droneId"]', 1);
    select('select[name="studentGroupId"]', 1);
    type('input[name="batteries"]', '4');
    type('input[name="propellers"]', '8');
    submit();

    const request = httpMock.expectOne(loansUrl);
    request.flush(
      { title: 'Conflict', detail: 'TELLO-07 is already loaned.', status: 409 },
      { status: 409, statusText: 'Conflict' }
    );
    fixture.detectChanges();

    expect(element('[role="alert"]').textContent?.trim()).toBe('TELLO-07 is already loaned.');
  });

  it('should list an active Loan as Group 3 | TELLO-07 | 4 | 8 | 19.08.2026', () => {
    load(available, groups, [activeLoan]);

    expect(rows()).toEqual([['Group 3', 'TELLO-07', '4', '8', '19.08.2026', 'Return']]);
  });

  it('should return a Loan, remove the row and offer the Drone again', () => {
    load(available, groups, [activeLoan]);

    (element('.loan-list tbody button') as HTMLButtonElement).click();
    fixture.detectChanges();

    const request = httpMock.expectOne(`${loansUrl}/1/return`);
    expect(request.request.method).toBe('POST');
    request.flush(activeLoan);
    fixture.detectChanges();

    expect(rows()).toEqual([['No active loans.']]);

    httpMock
      .expectOne(dronesUrl)
      .flush([
        { id: 1, name: 'TELLO-02', description: '', status: 'Loaned' },
        { id: 2, name: 'TELLO-07', description: '', status: 'Available' }
      ]);
    fixture.detectChanges();

    expect(options('select[name="droneId"]')).toEqual(['Select a drone', 'TELLO-07']);
  });

  it('should show the backend error when a Loan is already returned and keep the row', () => {
    load(available, groups, [activeLoan]);

    (element('.loan-list tbody button') as HTMLButtonElement).click();
    fixture.detectChanges();

    httpMock.expectOne(`${loansUrl}/1/return`).flush(
      { title: 'Conflict', detail: 'The Loan is already returned.', status: 409 },
      { status: 409, statusText: 'Conflict' }
    );
    fixture.detectChanges();

    expect(element('[role="alert"]').textContent?.trim()).toBe('The Loan is already returned.');
    expect(rows()).toEqual([['Group 3', 'TELLO-07', '4', '8', '19.08.2026', 'Return']]);
  });
});
