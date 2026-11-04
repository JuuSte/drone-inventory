import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CreateLoanRequest, Loan, LoanHistoryEntry } from '../models/loan.model';

@Injectable({ providedIn: 'root' })
export class LoanService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/loans`;

  getActive(): Observable<Loan[]> {
    return this.http.get<Loan[]>(`${this.baseUrl}/active`);
  }

  getHistory(): Observable<LoanHistoryEntry[]> {
    return this.http.get<LoanHistoryEntry[]>(`${this.baseUrl}/history`);
  }

  create(request: CreateLoanRequest): Observable<Loan> {
    return this.http.post<Loan>(this.baseUrl, request);
  }

  returnLoan(id: number): Observable<Loan> {
    return this.http.post<Loan>(`${this.baseUrl}/${id}/return`, {});
  }
}
