import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { Drone } from '../../models/drone.model';
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

  function load(drones: Drone[], groups: StudentGroup[]): void {
    httpMock.expectOne(dronesUrl).flush(drones);
    httpMock.expectOne(groupsUrl).flush(groups);
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
    element('form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  const available = [
    { id: 1, name: 'TELLO-02', description: '', status: 'Loaned' as const },
    { id: 2, name: 'TELLO-07', description: '', status: 'Available' as const }
  ];
  const groups = [{ id: 1, name: 'Group 3' }];

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
    request.flush({
      id: 1,
      studentGroupName: 'Group 3',
      droneName: 'TELLO-07',
      batteries: 4,
      propellers: 8,
      loanDate: '2026-08-19'
    });
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(element('[role="status"]').textContent?.trim()).toBe('Loan created.');
    expect((element('input[name="batteries"]') as HTMLInputElement).value).toBe('0');
    expect((element('input[name="propellers"]') as HTMLInputElement).value).toBe('0');
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
});
