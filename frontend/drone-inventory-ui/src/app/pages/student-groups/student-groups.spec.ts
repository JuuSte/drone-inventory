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

  // web-ui scenarios (UI-004, UI-006)

  function element<T extends Element = HTMLElement>(selector: string): T | null {
    return (fixture.nativeElement as HTMLElement).querySelector<T>(selector);
  }

  // GIVEN a StudentGroup named "Group 3" exists
  // WHEN the user creates another StudentGroup named "Group 3"
  // THEN the backend error message is shown in an error alert above the create form
  // AND the StudentGroups table is unchanged
  it('Backend error is shown above the form', () => {
    httpMock.expectOne(url).flush([{ id: 1, name: 'Group 3' }] satisfies StudentGroup[]);
    fixture.detectChanges();

    type('input[name="name"]', 'Group 3');
    submit();
    httpMock
      .expectOne(url)
      .flush(
        { title: 'Conflict', detail: 'A StudentGroup named "Group 3" already exists.', status: 409 },
        { status: 409, statusText: 'Conflict' }
      );
    fixture.detectChanges();

    const alert = element('[role="alert"]')!;
    const form = element('form')!;
    expect(alert.classList).toContain('alert--error');
    expect(alert.compareDocumentPosition(form) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(names()).toEqual(['Group 3']);
  });

  // WHEN the StudentGroups page with its form is shown
  // THEN every input has an associated label (and Name is marked as required)
  it('Every input has a label', () => {
    httpMock.expectOne(url).flush([]);

    const inputs = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('input'));
    expect(inputs.length).toBe(1);
    for (const input of inputs) {
      expect(input.labels?.length).toBe(1);
    }

    const name = element<HTMLInputElement>('input[name="name"]')!;
    expect(name.required).toBe(true);
    expect(name.labels?.[0].querySelector('.required-marker')).not.toBeNull();
    expect(element<HTMLFormElement>('form')!.noValidate).toBe(true);
  });

  // Short confirmation after a successful create (UI-005).
  it('shows a confirmation after a StudentGroup is created', () => {
    httpMock.expectOne(url).flush([]);

    type('input[name="name"]', 'Group 3');
    submit();
    httpMock.expectOne(url).flush({ id: 1, name: 'Group 3' } satisfies StudentGroup);
    fixture.detectChanges();

    expect(element('[role="status"]')?.textContent?.trim()).toBe('StudentGroup "Group 3" created.');
    expect(element('[role="alert"]')).toBeNull();
  });
});
