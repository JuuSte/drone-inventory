import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CreateDroneRequest, Drone } from '../models/drone.model';

@Injectable({ providedIn: 'root' })
export class DroneService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/drones`;

  getAll(): Observable<Drone[]> {
    return this.http.get<Drone[]>(this.baseUrl);
  }

  create(request: CreateDroneRequest): Observable<Drone> {
    return this.http.post<Drone>(this.baseUrl, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
