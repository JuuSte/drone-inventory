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
      ['TELLO-02', 'Loaned', 'no charger'],
      ['TELLO-07', 'Available', 'SSID: TELLO-9FA21B']
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
});
