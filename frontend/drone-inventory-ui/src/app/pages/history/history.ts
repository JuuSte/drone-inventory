import { Component, OnInit, inject, signal } from '@angular/core';
import { formatDate } from '../../core/date';
import { problemDetailsMessage } from '../../core/problem-details';
import { LoanHistoryEntry } from '../../models/loan.model';
import { LoanService } from '../../services/loan.service';

@Component({
  selector: 'app-history-page',
  template: `
    <h1>History</h1>

    @if (errorMessage(); as message) {
      <p class="error" role="alert">{{ message }}</p>
    }

    <table class="history-list">
      <thead>
        <tr>
          <th>Group</th>
          <th>Drone</th>
          <th>Batteries</th>
          <th>Propellers</th>
          <th>LoanDate</th>
          <th>ReturnDate</th>
        </tr>
      </thead>
      <tbody>
        @for (loan of loans(); track loan.id) {
          <tr>
            <td>{{ loan.studentGroupName }}</td>
            <td>{{ loan.droneName }}</td>
            <td>{{ loan.batteries }}</td>
            <td>{{ loan.propellers }}</td>
            <td>{{ formatDate(loan.loanDate) }}</td>
            <td>{{ formatDate(loan.returnDate) }}</td>
          </tr>
        } @empty {
          <tr>
            <td colspan="6">No returned loans yet.</td>
          </tr>
        }
      </tbody>
    </table>
  `,
  styles: `
    .error {
      padding: 0.5rem 0.75rem;
      border: 1px solid #e5a3a3;
      border-radius: 0.375rem;
      background: #fdf1f1;
      color: #a12a2a;
    }

    table {
      width: 100%;
      border-collapse: collapse;
    }

    th,
    td {
      padding: 0.5rem 0.75rem;
      border-bottom: 1px solid #dfe3ea;
      text-align: left;
    }

    th {
      font-size: 0.8125rem;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: #6b7488;
    }
  `
})
export class HistoryPage implements OnInit {
  private readonly loanService = inject(LoanService);

  readonly loans = signal<LoanHistoryEntry[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly formatDate = formatDate;

  ngOnInit(): void {
    this.loanService.getHistory().subscribe({
      next: (loans) => this.loans.set(loans),
      error: (error: unknown) => this.errorMessage.set(problemDetailsMessage(error))
    });
  }
}
