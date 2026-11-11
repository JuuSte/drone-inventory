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

    <section class="card" aria-labelledby="create-group-heading">
      <h2 id="create-group-heading">Create StudentGroup</h2>

      @if (errorMessage(); as message) {
        <p class="alert alert--error" role="alert">{{ message }}</p>
      }

      @if (successMessage(); as message) {
        <p class="alert alert--success" role="status">{{ message }}</p>
      }

      <!-- novalidate: validation stays in the backend, its message is shown above (design D5). -->
      <form class="create-form" novalidate (ngSubmit)="create()">
        <label class="form-field">
          <span>Name <span class="required-marker" aria-hidden="true">*</span></span>
          <input
            name="name"
            required
            autocomplete="off"
            [ngModel]="name()"
            (ngModelChange)="name.set($event)"
          />
        </label>
        <button type="submit" [disabled]="saving()">
          {{ saving() ? 'Saving…' : 'Create StudentGroup' }}
        </button>
      </form>
      <p class="hint"><span aria-hidden="true">*</span> Required field</p>
    </section>

    <section class="card" aria-labelledby="group-list-heading">
      <h2 id="group-list-heading">All StudentGroups</h2>

      <div class="table-wrap">
        <table class="student-group-list">
          <thead>
            <tr>
              <th scope="col">Name</th>
            </tr>
          </thead>
          <tbody>
            @for (group of studentGroups(); track group.id) {
              <tr>
                <td class="name">{{ group.name }}</td>
              </tr>
            } @empty {
              <tr>
                <td class="empty">No student groups yet.</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </section>
  `,
  styles: `
    .create-form {
      display: flex;
      flex-wrap: wrap;
      align-items: flex-end;
      gap: var(--space-3);
    }

    .create-form .form-field {
      flex: 1 1 200px;
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

    .empty {
      padding: var(--space-4) var(--space-3);
      text-align: center;
      color: var(--text-muted);
    }
  `
})
export class StudentGroupsPage implements OnInit {
  private readonly studentGroupService = inject(StudentGroupService);

  readonly studentGroups = signal<StudentGroup[]>([]);
  readonly name = signal('');
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly saving = signal(false);

  ngOnInit(): void {
    this.studentGroupService.getAll().subscribe({
      next: (groups) => this.studentGroups.set(groups),
      error: (error: unknown) => this.errorMessage.set(problemDetailsMessage(error))
    });
  }

  create(): void {
    // Clear the previous result before the new one is shown (UI-004).
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.saving.set(true);

    this.studentGroupService.create({ name: this.name() }).subscribe({
      next: (group) => {
        this.studentGroups.update((groups) => [...groups, group]);
        this.successMessage.set(`StudentGroup "${group.name}" created.`);
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
