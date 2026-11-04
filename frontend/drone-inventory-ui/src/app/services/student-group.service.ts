import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CreateStudentGroupRequest, StudentGroup } from '../models/student-group.model';

@Injectable({ providedIn: 'root' })
export class StudentGroupService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/student-groups`;

  getAll(): Observable<StudentGroup[]> {
    return this.http.get<StudentGroup[]>(this.baseUrl);
  }

  create(request: CreateStudentGroupRequest): Observable<StudentGroup> {
    return this.http.post<StudentGroup>(this.baseUrl, request);
  }
}
