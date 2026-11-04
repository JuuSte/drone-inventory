import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { problemDetailsMessage } from '../../core/problem-details';
import { StudentGroup } from '../../models/student-group.model';
import { StudentGroupService } from '../../services/student-group.service';

@Component({
  selector: 'app-student-groups-page',
  imports: [FormsModule],
  template: `
    <h1>Student Groups</h1>

    <form class="create-form" (ngSubmit)="create()">
      <label>
        Name
        <input name="name" [ngModel]="name()" (ngModelChange)="name.set($event)" />
      </label>
      <button type="submit" [disabled]="saving()">Create group</button>
    </form>

    @if (errorMessage(); as message) {
      <p class="error" role="alert">{{ message }}</p>
    }

    <table class="student-group-list">
      <thead>
        <tr>
          <th>Name</th>
        </tr>
      </thead>
      <tbody>
        @for (group of studentGroups(); track group.id) {
          <tr>
            <td>{{ group.name }}</td>
          </tr>
        } @empty {
          <tr>
            <td>No student groups yet.</td>
          </tr>
        }
      </tbody>
    </table>
  `,
  styles: `
    .create-form {
      display: flex;
      flex-wrap: wrap;
      align-items: flex-end;
      gap: 1rem;
      margin-bottom: 1rem;
    }

    .create-form label {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
      font-size: 0.875rem;
      color: #47506a;
    }

    input {
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
export class StudentGroupsPage implements OnInit {
  private readonly studentGroupService = inject(StudentGroupService);

  readonly studentGroups = signal<StudentGroup[]>([]);
  readonly name = signal('');
  readonly errorMessage = signal<string | null>(null);
  readonly saving = signal(false);

  ngOnInit(): void {
    this.studentGroupService.getAll().subscribe({
      next: (groups) => this.studentGroups.set(groups),
      error: (error: unknown) => this.errorMessage.set(problemDetailsMessage(error))
    });
  }

  create(): void {
    this.errorMessage.set(null);
    this.saving.set(true);

    this.studentGroupService.create({ name: this.name() }).subscribe({
      next: (group) => {
        this.studentGroups.update((groups) => [...groups, group]);
        this.name.set('');
        this.saving.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(problemDetailsMessage(error));
        this.saving.set(false);
      }
    });
  }
}
