import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { StudentGroup } from '../models/student-group.model';
import { StudentGroupService } from './student-group.service';

describe('StudentGroupService', () => {
  let service: StudentGroupService;
  let httpMock: HttpTestingController;
  const url = `${environment.apiBaseUrl}/student-groups`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(StudentGroupService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should list student groups from GET /api/student-groups', () => {
    const groups: StudentGroup[] = [
      { id: 1, name: 'Group 1' },
      { id: 2, name: 'Group 3' }
    ];
    let result: StudentGroup[] | undefined;

    service.getAll().subscribe((response) => (result = response));

    const request = httpMock.expectOne(url);
    expect(request.request.method).toBe('GET');
    request.flush(groups);
    expect(result).toEqual(groups);
  });

  it('should post a new student group to /api/student-groups', () => {
    const created: StudentGroup = { id: 3, name: 'Group 3' };
    let result: StudentGroup | undefined;

    service.create({ name: 'Group 3' }).subscribe((response) => (result = response));

    const request = httpMock.expectOne(url);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Group 3' });
    request.flush(created);
    expect(result).toEqual(created);
  });

  it('should delete a student group with DELETE /api/student-groups/{id}', () => {
    let completed = false;

    service.delete(2).subscribe({ complete: () => (completed = true) });

    const request = httpMock.expectOne(`${url}/2`);
    expect(request.request.method).toBe('DELETE');
    expect(request.request.body).toBeNull();
    request.flush(null, { status: 204, statusText: 'No Content' });
    expect(completed).toBe(true);
  });
});
