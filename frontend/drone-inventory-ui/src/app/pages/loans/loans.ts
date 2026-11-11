import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { formatDate } from '../../core/date';
import { problemDetailsMessage } from '../../core/problem-details';
import { Drone } from '../../models/drone.model';
import { Loan } from '../../models/loan.model';
import { StudentGroup } from '../../models/student-group.model';
import { DroneService } from '../../services/drone.service';
import { LoanService } from '../../services/loan.service';
import { StudentGroupService } from '../../services/student-group.service';
import { StatusBadge } from '../../components/status-badge/status-badge';

@Component({
  selector: 'app-loans-page',
  imports: [FormsModule, StatusBadge],
  template: `
    <h1>Loans</h1>

    <section class="card" aria-labelledby="create-loan-heading">
      <h2 id="create-loan-heading">Create Loan</h2>

      @if (errorMessage(); as message) {
        <p class="alert alert--error" role="alert">{{ message }}</p>
      }

      @if (successMessage(); as message) {
        <p class="alert alert--success" role="status">{{ message }}</p>
      }

      <!-- novalidate: validation stays in the backend, its message is shown above (design D5). -->
      <form class="loan-form" novalidate (ngSubmit)="create()">
        <label class="form-field">
          <span>Drone <span class="required-marker" aria-hidden="true">*</span></span>
          <select
            name="droneId"
            required
            [ngModel]="droneId()"
            (ngModelChange)="droneId.set($event)"
          >
            <option [ngValue]="null">Select a Drone</option>
            @for (drone of availableDrones(); track drone.id) {
              <option [ngValue]="drone.id">{{ drone.name }}</option>
            }
          </select>
        </label>
        <label class="form-field">
          <span>StudentGroup <span class="required-marker" aria-hidden="true">*</span></span>
          <select
            name="studentGroupId"
            required
            [ngModel]="studentGroupId()"
            (ngModelChange)="studentGroupId.set($event)"
          >
            <option [ngValue]="null">Select a StudentGroup</option>
            @for (group of studentGroups(); track group.id) {
              <option [ngValue]="group.id">{{ group.name }}</option>
            }
          </select>
        </label>
        <label class="form-field quantity-field">
          <span>Batteries</span>
          <input
            type="number"
            name="batteries"
            min="0"
            step="1"
            inputmode="numeric"
            [ngModel]="batteries()"
            (ngModelChange)="batteries.set($event)"
          />
        </label>
        <label class="form-field quantity-field">
          <span>Propellers</span>
          <input
            type="number"
            name="propellers"
            min="0"
            step="1"
            inputmode="numeric"
            [ngModel]="propellers()"
            (ngModelChange)="propellers.set($event)"
          />
        </label>
        <button type="submit" [disabled]="!droneId() || !studentGroupId() || saving()">
          {{ saving() ? 'Saving…' : 'Create Loan' }}
        </button>
      </form>
      <p class="hint"><span aria-hidden="true">*</span> Required field</p>
    </section>

    <section class="card" aria-labelledby="active-loans-heading">
      <h2 id="active-loans-heading">Active Loans</h2>

      @if (returnErrorMessage(); as message) {
        <p class="alert alert--error" role="alert">{{ message }}</p>
      }

      @if (returnSuccessMessage(); as message) {
        <p class="alert alert--success" role="status">{{ message }}</p>
      }

      <div class="table-wrap">
        <table class="loan-list">
          <thead>
            <tr>
              <th scope="col">StudentGroup</th>
              <th scope="col">Drone</th>
              <th scope="col" class="num">Batteries</th>
              <th scope="col" class="num">Propellers</th>
              <th scope="col">LoanDate</th>
              <th scope="col">Status</th>
              <th scope="col"><span class="visually-hidden">Actions</span></th>
            </tr>
          </thead>
          <tbody>
            @for (loan of activeLoans(); track loan.id) {
              <tr>
                <td>{{ loan.studentGroupName }}</td>
                <td class="name">{{ loan.droneName }}</td>
                <td class="num">{{ loan.batteries }}</td>
                <td class="num">{{ loan.propellers }}</td>
                <td class="date">{{ formatDate(loan.loanDate) }}</td>
                <!-- /api/loans/active only returns Loans with Status Active. -->
                <td><app-status-badge status="Active" /></td>
                <td class="actions">
                  <button
                    type="button"
                    class="button--secondary"
                    [attr.aria-label]="'Return Loan of ' + loan.droneName"
                    [disabled]="returningId() === loan.id"
                    (click)="returnLoan(loan.id)"
                  >
                    Return Loan
                  </button>
                </td>
              </tr>
            } @empty {
              <tr>
                <td class="empty" colspan="7">No active loans.</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </section>
  `,
  styles: `
    .loan-form {
      display: flex;
      flex-wrap: wrap;
      align-items: flex-end;
      gap: var(--space-3);
    }

    .loan-form .form-field {
      flex: 1 1 180px;
    }

    .loan-form .quantity-field {
      flex: 0 1 120px;
    }

    .required-marker {
      color: var(--danger);
    }

    .hint {
      margin: var(--space-2) 0 0;
      font-size: 0.8125rem;
      color: var(--text-muted);
    }

    .name {
      font-weight: 600;
    }

    .date {
      font-variant-numeric: tabular-nums;
      white-space: nowrap;
    }

    .actions {
      text-align: right;
      white-space: nowrap;
    }

    .button--secondary {
      border-color: var(--primary);
      background: var(--surface);
      color: var(--primary);
    }

    .button--secondary:hover:not(:disabled) {
      background: var(--bg);
    }

    .empty {
      padding: var(--space-4) var(--space-3);
      text-align: center;
      color: var(--text-muted);
    }

    .visually-hidden {
      position: absolute;
      width: 1px;
      height: 1px;
      overflow: hidden;
      clip-path: inset(50%);
      white-space: nowrap;
    }
  `
})
export class LoansPage implements OnInit {
  private readonly droneService = inject(DroneService);
  private readonly studentGroupService = inject(StudentGroupService);
  private readonly loanService = inject(LoanService);

  readonly drones = signal<Drone[]>([]);
  readonly studentGroups = signal<StudentGroup[]>([]);
  readonly activeLoans = signal<Loan[]>([]);
  readonly availableDrones = computed(() =>
    this.drones().filter((drone) => drone.status === 'Available')
  );

  readonly droneId = signal<number | null>(null);
  readonly studentGroupId = signal<number | null>(null);
  readonly batteries = signal(0);
  readonly propellers = signal(0);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly saving = signal(false);
  readonly returnErrorMessage = signal<string | null>(null);
  readonly returnSuccessMessage = signal<string | null>(null);
  readonly returningId = signal<number | null>(null);
  readonly formatDate = formatDate;

  ngOnInit(): void {
    this.reloadDrones();
    this.studentGroupService.getAll().subscribe({
      next: (groups) => this.studentGroups.set(groups),
      error: (error: unknown) => this.errorMessage.set(problemDetailsMessage(error))
    });
    this.reloadActiveLoans();
  }

  create(): void {
    const droneId = this.droneId();
    const studentGroupId = this.studentGroupId();
    if (droneId === null || studentGroupId === null) {
      return;
    }

    // Clear the previous result before the new one is shown (UI-004).
    this.clearMessages();
    this.saving.set(true);

    this.loanService
      .create({
        droneId,
        studentGroupId,
        batteries: this.batteries(),
        propellers: this.propellers()
      })
      .subscribe({
        next: () => {
          this.successMessage.set('Loan created.');
          this.batteries.set(0);
          this.propellers.set(0);
          this.saving.set(false);
          this.reloadActiveLoans();
        },
        error: (error: unknown) => {
          this.errorMessage.set(problemDetailsMessage(error));
          this.saving.set(false);
        }
      });
  }

  returnLoan(id: number): void {
    const loan = this.activeLoans().find((l) => l.id === id);
    this.clearMessages();
    this.returningId.set(id);

    this.loanService.returnLoan(id).subscribe({
      next: () => {
        this.activeLoans.update((loans) => loans.filter((l) => l.id !== id));
        this.returnSuccessMessage.set(
          loan ? `Loan of ${loan.droneName} to ${loan.studentGroupName} returned.` : 'Loan returned.'
        );
        this.returningId.set(null);
        this.reloadDrones();
      },
      error: (error: unknown) => {
        this.returnErrorMessage.set(problemDetailsMessage(error));
        this.returningId.set(null);
      }
    });
  }

  private clearMessages(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.returnErrorMessage.set(null);
    this.returnSuccessMessage.set(null);
  }

  private reloadDrones(): void {
    this.droneService.getAll().subscribe({
      next: (drones) => this.drones.set(drones),
      error: (error: unknown) => this.errorMessage.set(problemDetailsMessage(error))
    });
  }

  private reloadActiveLoans(): void {
    this.loanService.getActive().subscribe({
      next: (loans) => this.activeLoans.set(loans),
      error: (error: unknown) => this.errorMessage.set(problemDetailsMessage(error))
    });
  }
}
