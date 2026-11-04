import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { problemDetailsMessage } from '../../core/problem-details';
import { Drone } from '../../models/drone.model';
import { Loan } from '../../models/loan.model';
import { StudentGroup } from '../../models/student-group.model';
import { DroneService } from '../../services/drone.service';
import { LoanService } from '../../services/loan.service';
import { StudentGroupService } from '../../services/student-group.service';

/** Formats an ISO `yyyy-MM-dd` date as `dd.MM.yyyy` without going through `Date`. */
export function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-');
  return year && month && day ? `${day}.${month}.${year}` : isoDate;
}

@Component({
  selector: 'app-loans-page',
  imports: [FormsModule],
  template: `
    <h1>Loans</h1>

    <h2>Loan a drone</h2>

    <form class="loan-form" (ngSubmit)="create()">
      <label>
        Drone
        <select name="droneId" [ngModel]="droneId()" (ngModelChange)="droneId.set($event)">
          <option [ngValue]="null">Select a drone</option>
          @for (drone of availableDrones(); track drone.id) {
            <option [ngValue]="drone.id">{{ drone.name }}</option>
          }
        </select>
      </label>
      <label>
        Student group
        <select
          name="studentGroupId"
          [ngModel]="studentGroupId()"
          (ngModelChange)="studentGroupId.set($event)"
        >
          <option [ngValue]="null">Select a group</option>
          @for (group of studentGroups(); track group.id) {
            <option [ngValue]="group.id">{{ group.name }}</option>
          }
        </select>
      </label>
      <label>
        Batteries
        <input
          type="number"
          name="batteries"
          min="0"
          [ngModel]="batteries()"
          (ngModelChange)="batteries.set($event)"
        />
      </label>
      <label>
        Propellers
        <input
          type="number"
          name="propellers"
          min="0"
          [ngModel]="propellers()"
          (ngModelChange)="propellers.set($event)"
        />
      </label>
      <button type="submit" [disabled]="!droneId() || !studentGroupId() || saving()">Loan</button>
    </form>

    @if (errorMessage(); as message) {
      <p class="error" role="alert">{{ message }}</p>
    }

    @if (successMessage(); as message) {
      <p class="success" role="status">{{ message }}</p>
    }

    <h2>Active loans</h2>

    <table class="loan-list">
      <thead>
        <tr>
          <th>Group</th>
          <th>Drone</th>
          <th>Batteries</th>
          <th>Propellers</th>
          <th>LoanDate</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        @for (loan of activeLoans(); track loan.id) {
          <tr>
            <td>{{ loan.studentGroupName }}</td>
            <td>{{ loan.droneName }}</td>
            <td>{{ loan.batteries }}</td>
            <td>{{ loan.propellers }}</td>
            <td>{{ formatDate(loan.loanDate) }}</td>
            <td>
              <button type="button" (click)="returnLoan(loan.id)">Return</button>
            </td>
          </tr>
        } @empty {
          <tr>
            <td colspan="6">No active loans.</td>
          </tr>
        }
      </tbody>
    </table>
  `,
  styles: `
    .loan-form {
      display: flex;
      flex-wrap: wrap;
      align-items: flex-end;
      gap: 1rem;
      margin-bottom: 1rem;
    }

    .loan-form label {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
      font-size: 0.875rem;
      color: #47506a;
    }

    input,
    select {
      padding: 0.375rem 0.5rem;
      border: 1px solid #dfe3ea;
      border-radius: 0.375rem;
      font: inherit;
      color: #1f2430;
    }

    button {
      padding: 0.5rem 1rem;
      border: 0;
      border-radius: 0.375rem;
      background: #2f6feb;
      color: #fff;
      font: inherit;
      cursor: pointer;
    }

    button:disabled {
      background: #9db6e8;
      cursor: default;
    }

    .error {
      padding: 0.5rem 0.75rem;
      border: 1px solid #e5a3a3;
      border-radius: 0.375rem;
      background: #fdf1f1;
      color: #a12a2a;
    }

    .success {
      padding: 0.5rem 0.75rem;
      border: 1px solid #a3c7a9;
      border-radius: 0.375rem;
      background: #f1fdf3;
      color: #2f7d4f;
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

    .loan-list button {
      padding: 0.25rem 0.75rem;
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

    this.errorMessage.set(null);
    this.successMessage.set(null);
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
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.loanService.returnLoan(id).subscribe({
      next: () => {
        this.activeLoans.update((loans) => loans.filter((loan) => loan.id !== id));
        this.reloadDrones();
      },
      error: (error: unknown) => this.errorMessage.set(problemDetailsMessage(error))
    });
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
