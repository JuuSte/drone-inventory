import { Component, OnInit, inject, signal } from '@angular/core';
import { formatDate } from '../../core/date';
import { problemDetailsMessage } from '../../core/problem-details';
import { LoanHistoryEntry } from '../../models/loan.model';
import { LoanService } from '../../services/loan.service';
import { StatusBadge } from '../../components/status-badge/status-badge';

@Component({
  selector: 'app-history-page',
  imports: [StatusBadge],
  template: `
    <h1>History</h1>

    <section class="card" aria-labelledby="returned-loans-heading">
      <h2 id="returned-loans-heading">Returned Loans</h2>

      @if (errorMessage(); as message) {
        <p class="alert alert--error" role="alert">{{ message }}</p>
      }

      <div class="table-wrap">
        <table class="history-list">
          <thead>
            <tr>
              <th scope="col">StudentGroup</th>
              <th scope="col">Drone</th>
              <th scope="col" class="num">Batteries</th>
              <th scope="col" class="num">Propellers</th>
              <th scope="col">LoanDate</th>
              <th scope="col">ReturnDate</th>
              <th scope="col">Status</th>
            </tr>
          </thead>
          <tbody>
            @for (loan of loans(); track loan.id) {
              <tr>
                <td>{{ loan.studentGroupName }}</td>
                <td class="name">{{ loan.droneName }}</td>
                <td class="num">{{ loan.batteries }}</td>
                <td class="num">{{ loan.propellers }}</td>
                <td class="date">{{ formatDate(loan.loanDate) }}</td>
                <td class="date">{{ formatDate(loan.returnDate) }}</td>
                <!-- /api/loans/history only returns Loans with Status Returned. -->
                <td><app-status-badge status="Returned" /></td>
              </tr>
            } @empty {
              <tr>
                <td class="empty" colspan="7">No returned loans yet.</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </section>
  `,
  styles: `
    .name {
      font-weight: 600;
    }

    .date {
      font-variant-numeric: tabular-nums;
      white-space: nowrap;
    }

    .empty {
      padding: var(--space-4) var(--space-3);
      text-align: center;
      color: var(--text-muted);
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
