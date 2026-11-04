import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { StudentGroup } from '../../models/student-group.model';
import { StudentGroupsPage } from './student-groups';

describe('StudentGroupsPage', () => {
  let fixture: ComponentFixture<StudentGroupsPage>;
  let httpMock: HttpTestingController;
  const url = `${environment.apiBaseUrl}/student-groups`;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StudentGroupsPage],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    fixture = TestBed.createComponent(StudentGroupsPage);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  function text(): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  function type(selector: string, value: string): void {
    const input = (fixture.nativeElement as HTMLElement).querySelector(selector) as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function submit(): void {
    const form = (fixture.nativeElement as HTMLElement).querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  function names(): (string | undefined)[] {
    return Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr')).map(
      (row) => row.querySelector('td')?.textContent?.trim()
    );
  }

  it('should list StudentGroups with their Name', () => {
    const groups: StudentGroup[] = [
      { id: 1, name: 'Group 1' },
      { id: 2, name: 'Group 3' }
    ];

    httpMock.expectOne(url).flush(groups);
    fixture.detectChanges();

    expect(names()).toEqual(['Group 1', 'Group 3']);
  });

  it('should create Group 3', async () => {
    httpMock.expectOne(url).flush([]);

    type('input[name="name"]', 'Group 3');
    submit();

    const request = httpMock.expectOne(url);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Group 3' });
    request.flush({ id: 1, name: 'Group 3' });
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(names()).toEqual(['Group 3']);
    const nameInput = (fixture.nativeElement as HTMLElement).querySelector(
      'input[name="name"]'
    ) as HTMLInputElement;
    expect(nameInput.value).toBe('');
  });

  it('should show the backend error when the name is a duplicate', () => {
    httpMock.expectOne(url).flush([{ id: 1, name: 'Group 3' }]);
    fixture.detectChanges();

    type('input[name="name"]', 'Group 3');
    submit();

    const request = httpMock.expectOne(url);
    request.flush(
      { title: 'Conflict', detail: 'A StudentGroup named "Group 3" already exists.', status: 409 },
      { status: 409, statusText: 'Conflict' }
    );
    fixture.detectChanges();

    const alert = (fixture.nativeElement as HTMLElement).querySelector('[role="alert"]');
    expect(alert?.textContent?.trim()).toBe('A StudentGroup named "Group 3" already exists.');
    expect(names()).toEqual(['Group 3']);
    expect(text()).not.toContain('No student groups yet.');
  });

  it('should show the backend error for an empty name', () => {
    httpMock.expectOne(url).flush([]);

    submit();

    const request = httpMock.expectOne(url);
    request.flush(
      { title: 'Bad Request', detail: 'Name is required.', status: 400 },
      { status: 400, statusText: 'Bad Request' }
    );
    fixture.detectChanges();

    const alert = (fixture.nativeElement as HTMLElement).querySelector('[role="alert"]');
    expect(alert?.textContent?.trim()).toBe('Name is required.');
    expect(names()).toEqual(['No student groups yet.']);
  });
});
