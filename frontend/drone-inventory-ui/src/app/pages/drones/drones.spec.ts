import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { Drone } from '../../models/drone.model';
import { DronesPage } from './drones';

describe('DronesPage', () => {
  let fixture: ComponentFixture<DronesPage>;
  let httpMock: HttpTestingController;
  const url = `${environment.apiBaseUrl}/drones`;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DronesPage],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    fixture = TestBed.createComponent(DronesPage);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  function text(): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  function type(selector: string, value: string): void {
    const input = (fixture.nativeElement as HTMLElement).querySelector(selector) as
      | HTMLInputElement
      | HTMLTextAreaElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function submit(): void {
    const form = (fixture.nativeElement as HTMLElement).querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  it('should list drones with Name, status and Description', () => {
    const drones: Drone[] = [
      { id: 1, name: 'TELLO-02', description: 'no charger', status: 'Loaned' },
      { id: 2, name: 'TELLO-07', description: 'SSID: TELLO-9FA21B', status: 'Available' }
    ];

    httpMock.expectOne(url).flush(drones);
    fixture.detectChanges();

    const rows = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr')).map(
      (row) => Array.from(row.querySelectorAll('td')).map((cell) => cell.textContent?.trim())
    );
    expect(rows).toEqual([
      ['TELLO-02', 'Loaned', 'no charger', 'Delete'],
      ['TELLO-07', 'Available', 'SSID: TELLO-9FA21B', 'Delete']
    ]);
  });

  it('should create TELLO-07 and show it as Available', async () => {
    httpMock.expectOne(url).flush([]);

    type('input[name="name"]', 'TELLO-07');
    type('textarea[name="description"]', 'SSID: TELLO-9FA21B, right propeller guard damaged');
    submit();

    const request = httpMock.expectOne(url);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      name: 'TELLO-07',
      description: 'SSID: TELLO-9FA21B, right propeller guard damaged'
    });
    request.flush({
      id: 1,
      name: 'TELLO-07',
      description: 'SSID: TELLO-9FA21B, right propeller guard damaged',
      status: 'Available'
    });
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(text()).toContain('TELLO-07');
    expect(text()).toContain('Available');
    const nameInput = (fixture.nativeElement as HTMLElement).querySelector(
      'input[name="name"]'
    ) as HTMLInputElement;
    expect(nameInput.value).toBe('');
  });

  it('should show the backend error for an empty name', () => {
    httpMock.expectOne(url).flush([]);

    type('textarea[name="description"]', 'no name given');
    submit();

    const request = httpMock.expectOne(url);
    request.flush(
      { title: 'Bad Request', detail: 'Name is required.', status: 400 },
      { status: 400, statusText: 'Bad Request' }
    );
    fixture.detectChanges();

    const alert = (fixture.nativeElement as HTMLElement).querySelector('[role="alert"]');
    expect(alert?.textContent?.trim()).toBe('Name is required.');
    expect((fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr').length).toBe(1);
  });

  // web-ui scenarios (UI-002 .. UI-005)

  function element<T extends Element = HTMLElement>(selector: string): T | null {
    return (fixture.nativeElement as HTMLElement).querySelector<T>(selector);
  }

  function rowsText(): (string | undefined)[][] {
    return Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('.drone-list tbody tr')).map(
      (row) => Array.from(row.querySelectorAll('td')).map((cell) => cell.textContent?.trim())
    );
  }

  function failWith(detail: string, status = 409): void {
    httpMock
      .expectOne(url)
      .flush({ title: 'Conflict', detail, status }, { status, statusText: 'Error' });
    fixture.detectChanges();
  }

  // GIVEN the Drone "TELLO-07" is loaned and the Drone "TELLO-08" is not
  // WHEN the Drones page is shown
  // THEN "TELLO-07" shows the badge "Loaned" in the warning style
  // AND "TELLO-08" shows the badge "Available" in the success style
  it('Available and Loaned Drones have different badges', () => {
    httpMock.expectOne(url).flush([
      { id: 1, name: 'TELLO-07', description: '', status: 'Loaned' },
      { id: 2, name: 'TELLO-08', description: '', status: 'Available' }
    ] satisfies Drone[]);
    fixture.detectChanges();

    const badges = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('.drone-list tbody tr .badge')
    );
    expect(badges.map((b) => b.textContent?.trim())).toEqual(['Loaned', 'Available']);
    expect(badges[0].classList).toContain('badge--warning');
    expect(badges[1].classList).toContain('badge--success');
  });

  // GIVEN a Drone named "TELLO-07" exists
  // WHEN the user creates another Drone named "TELLO-07"
  // THEN the backend error message is shown in an error alert above the create form
  // AND the Drones table is unchanged
  it('Backend error is shown above the form', () => {
    httpMock.expectOne(url).flush([{ id: 1, name: 'TELLO-07', description: '', status: 'Available' }]);
    fixture.detectChanges();
    const before = rowsText();

    type('input[name="name"]', 'TELLO-07');
    submit();
    failWith('A Drone named "TELLO-07" already exists.');

    const alert = element('[role="alert"]')!;
    const form = element('form')!;
    expect(alert.textContent?.trim()).toBe('A Drone named "TELLO-07" already exists.');
    expect(alert.classList).toContain('alert--error');
    expect(alert.compareDocumentPosition(form) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(rowsText()).toEqual(before);
  });

  // GIVEN an error alert is shown on the Drones page
  // WHEN the user submits the create form again
  // THEN the previous error alert is removed before the new result is shown
  it('Error is cleared on the next submit', () => {
    httpMock.expectOne(url).flush([]);
    submit();
    failWith('Name is required.', 400);
    expect(element('[role="alert"]')).not.toBeNull();

    type('input[name="name"]', 'TELLO-07');
    submit();

    expect(element('[role="alert"]')).toBeNull();
    httpMock
      .expectOne(url)
      .flush({ id: 1, name: 'TELLO-07', description: '', status: 'Available' } satisfies Drone);
    fixture.detectChanges();
    expect(element('[role="alert"]')).toBeNull();
    expect(element('[role="status"]')?.textContent?.trim()).toBe('Drone "TELLO-07" created.');
  });

  // GIVEN the user submitted the Drone create form
  // WHEN the request has not finished yet
  // THEN the submit button is disabled
  it('Button is disabled while saving', () => {
    httpMock.expectOne(url).flush([]);
    const button = element<HTMLButtonElement>('form button[type="submit"]')!;
    expect(button.disabled).toBe(false);

    type('input[name="name"]', 'TELLO-07');
    submit();
    expect(button.disabled).toBe(true);

    httpMock
      .expectOne(url)
      .flush({ id: 1, name: 'TELLO-07', description: '', status: 'Available' } satisfies Drone);
    fixture.detectChanges();
    expect(button.disabled).toBe(false);
  });

  // WHEN the Drone create form is shown
  // THEN the Name field is marked as required and the Description field is not
  it('Required fields are marked', () => {
    httpMock.expectOne(url).flush([]);

    const name = element<HTMLInputElement>('input[name="name"]')!;
    const description = element<HTMLTextAreaElement>('textarea[name="description"]')!;
    expect(name.required).toBe(true);
    expect(name.labels?.[0].querySelector('.required-marker')).not.toBeNull();
    expect(description.required).toBe(false);
    expect(description.labels?.[0].querySelector('.required-marker')).toBeNull();
    // Validation stays in the backend: the browser must not block the submit.
    expect(element<HTMLFormElement>('form')!.noValidate).toBe(true);
  });

  // Every input has an associated label (UI-006).
  it('Every input has a label', () => {
    httpMock.expectOne(url).flush([]);

    const fields = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLInputElement | HTMLTextAreaElement>(
        'input, textarea'
      )
    );
    expect(fields.length).toBe(2);
    for (const field of fields) {
      expect(field.labels?.length).toBe(1);
    }
  });

  // GIVEN no Drones exist
  // WHEN the Drones page is shown
  // THEN the table shows the text "No drones yet."
  it('Empty list', () => {
    httpMock.expectOne(url).flush([]);
    fixture.detectChanges();

    expect(rowsText()).toEqual([['No drones yet.']]);
  });

  // web-ui scenarios (UI-008)

  function deleteButton(name: string): HTMLButtonElement {
    return element<HTMLButtonElement>(`button[aria-label="Delete Drone ${name}"]`)!;
  }

  function flushDrones(drones: Drone[]): void {
    httpMock.expectOne(url).flush(drones);
    fixture.detectChanges();
  }

  // GIVEN the Drone "TELLO-08" has no Loans
  // WHEN the user presses "Delete" in the row of "TELLO-08"
  // THEN the row of "TELLO-08" is removed from the Drones table
  // AND a confirmation is shown
  it('Drone is deleted from the Drones page', () => {
    flushDrones([
      { id: 1, name: 'TELLO-07', description: '', status: 'Available' },
      { id: 2, name: 'TELLO-08', description: '', status: 'Available' }
    ]);

    deleteButton('TELLO-08').click();
    fixture.detectChanges();

    const request = httpMock.expectOne(`${url}/2`);
    expect(request.request.method).toBe('DELETE');
    request.flush(null, { status: 204, statusText: 'No Content' });
    fixture.detectChanges();

    expect(rowsText().map((row) => row[0])).toEqual(['TELLO-07']);
    const confirmation = element('[role="status"]')!;
    expect(confirmation.textContent?.trim()).toBe('Drone "TELLO-08" deleted.');
    expect(confirmation.classList).toContain('alert--success');
  });

  // GIVEN the Drone "TELLO-07" has at least one Loan
  // WHEN the user presses "Delete" in the row of "TELLO-07"
  // THEN the backend error message is shown in an error alert
  // AND the row of "TELLO-07" is still in the Drones table
  it('Rejected delete keeps the row', () => {
    flushDrones([{ id: 1, name: 'TELLO-07', description: '', status: 'Loaned' }]);

    deleteButton('TELLO-07').click();
    fixture.detectChanges();

    httpMock.expectOne(`${url}/1`).flush(
      {
        title: 'Conflict',
        detail: 'Drone "TELLO-07" cannot be deleted because it has Loans.',
        status: 409
      },
      { status: 409, statusText: 'Conflict' }
    );
    fixture.detectChanges();

    const alert = element('[role="alert"]')!;
    expect(alert.textContent?.trim()).toBe('Drone "TELLO-07" cannot be deleted because it has Loans.');
    expect(alert.classList).toContain('alert--error');
    expect(rowsText().map((row) => row[0])).toEqual(['TELLO-07']);
  });

  // WHEN the Drones page lists "TELLO-07"
  // THEN the Delete button of that row has the accessible name "Delete Drone TELLO-07"
  it('Delete button names its row', () => {
    flushDrones([{ id: 1, name: 'TELLO-07', description: '', status: 'Available' }]);

    const button = deleteButton('TELLO-07');
    expect(button.getAttribute('aria-label')).toBe('Delete Drone TELLO-07');
    expect(button.textContent?.trim()).toBe('Delete');
  });

  // GIVEN the user pressed "Delete" in the row of "TELLO-08"
  // WHEN the request has not finished yet
  // THEN the Delete button of that row is disabled
  it('Button is disabled while deleting', () => {
    flushDrones([
      { id: 1, name: 'TELLO-07', description: '', status: 'Available' },
      { id: 2, name: 'TELLO-08', description: '', status: 'Available' }
    ]);
    const button = deleteButton('TELLO-08');
    expect(button.disabled).toBe(false);

    button.click();
    fixture.detectChanges();
    expect(deleteButton('TELLO-08').disabled).toBe(true);
    expect(deleteButton('TELLO-07').disabled).toBe(false);

    httpMock.expectOne(`${url}/2`).flush(null, { status: 204, statusText: 'No Content' });
    fixture.detectChanges();
    expect(rowsText().map((row) => row[0])).toEqual(['TELLO-07']);
  });
});
