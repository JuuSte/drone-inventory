import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { Drone } from '../models/drone.model';
import { DroneService } from './drone.service';

describe('DroneService', () => {
  let service: DroneService;
  let httpMock: HttpTestingController;
  const url = `${environment.apiBaseUrl}/drones`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(DroneService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should list drones from GET /api/drones', () => {
    const drones: Drone[] = [
      { id: 1, name: 'TELLO-02', description: '', status: 'Loaned' },
      { id: 2, name: 'TELLO-07', description: 'SSID: TELLO-9FA21B', status: 'Available' }
    ];
    let result: Drone[] | undefined;

    service.getAll().subscribe((response) => (result = response));

    const request = httpMock.expectOne(url);
    expect(request.request.method).toBe('GET');
    request.flush(drones);
    expect(result).toEqual(drones);
  });

  it('should post a new drone to /api/drones', () => {
    const created: Drone = {
      id: 3,
      name: 'TELLO-07',
      description: 'right propeller guard damaged',
      status: 'Available'
    };
    const body = { name: 'TELLO-07', description: 'right propeller guard damaged' };
    let result: Drone | undefined;

    service.create(body).subscribe((response) => (result = response));

    const request = httpMock.expectOne(url);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush(created);
    expect(result).toEqual(created);
  });

  it('should delete a drone with DELETE /api/drones/{id}', () => {
    let completed = false;

    service.delete(3).subscribe({ complete: () => (completed = true) });

    const request = httpMock.expectOne(`${url}/3`);
    expect(request.request.method).toBe('DELETE');
    expect(request.request.body).toBeNull();
    request.flush(null, { status: 204, statusText: 'No Content' });
    expect(completed).toBe(true);
  });
});
