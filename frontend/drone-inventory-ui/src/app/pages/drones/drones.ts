import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { problemDetailsMessage } from '../../core/problem-details';
import { Drone } from '../../models/drone.model';
import { DroneService } from '../../services/drone.service';
import { StatusBadge } from '../../components/status-badge/status-badge';

@Component({
  selector: 'app-drones-page',
  imports: [FormsModule, StatusBadge],
  template: `
    <h1>Drones</h1>

    <section class="card" aria-labelledby="create-drone-heading">
      <h2 id="create-drone-heading">Create Drone</h2>

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
        <label class="form-field description-field">
          <span>Description</span>
          <textarea
            name="description"
            rows="2"
            [ngModel]="description()"
            (ngModelChange)="description.set($event)"
          ></textarea>
        </label>
        <button type="submit" [disabled]="saving()">
          {{ saving() ? 'Saving…' : 'Create Drone' }}
        </button>
      </form>
      <p class="hint"><span aria-hidden="true">*</span> Required field</p>
    </section>

    <section class="card" aria-labelledby="drone-list-heading">
      <h2 id="drone-list-heading">All Drones</h2>

      @if (deleteErrorMessage(); as message) {
        <p class="alert alert--error" role="alert">{{ message }}</p>
      }

      @if (deleteSuccessMessage(); as message) {
        <p class="alert alert--success" role="status">{{ message }}</p>
      }

      <div class="table-wrap">
        <table class="drone-list">
          <thead>
            <tr>
              <th scope="col">Name</th>
              <th scope="col">Status</th>
              <th scope="col">Description</th>
              <th scope="col"><span class="visually-hidden">Actions</span></th>
            </tr>
          </thead>
          <tbody>
            @for (drone of drones(); track drone.id) {
              <tr>
                <td class="name">{{ drone.name }}</td>
                <td><app-status-badge [status]="drone.status" /></td>
                <td>{{ drone.description }}</td>
                <td class="actions">
                  <button
                    type="button"
                    class="button--secondary"
                    [attr.aria-label]="'Delete Drone ' + drone.name"
                    [disabled]="deletingId() === drone.id"
                    (click)="deleteDrone(drone)"
                  >
                    Delete
                  </button>
                </td>
              </tr>
            } @empty {
              <tr>
                <td class="empty" colspan="4">No drones yet.</td>
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

    .create-form .description-field {
      flex-grow: 2;
    }

    textarea {
      resize: vertical;
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

    .visually-hidden {
      position: absolute;
      width: 1px;
      height: 1px;
      overflow: hidden;
      clip-path: inset(50%);
      white-space: nowrap;
    }

    .empty {
      padding: var(--space-4) var(--space-3);
      text-align: center;
      color: var(--text-muted);
    }
  `
})
export class DronesPage implements OnInit {
  private readonly droneService = inject(DroneService);

  readonly drones = signal<Drone[]>([]);
  readonly name = signal('');
  readonly description = signal('');
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly saving = signal(false);
  readonly deletingId = signal<number | null>(null);
  readonly deleteErrorMessage = signal<string | null>(null);
  readonly deleteSuccessMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.droneService.getAll().subscribe({
      next: (drones) => this.drones.set(drones),
      error: (error: unknown) => this.errorMessage.set(problemDetailsMessage(error))
    });
  }

  create(): void {
    // Clear the previous result before the new one is shown (UI-004).
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.saving.set(true);

    this.droneService.create({ name: this.name(), description: this.description() }).subscribe({
      next: (drone) => {
        this.drones.update((drones) => [...drones, drone]);
        this.successMessage.set(`Drone "${drone.name}" created.`);
        this.name.set('');
        this.description.set('');
        this.saving.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(problemDetailsMessage(error));
        this.saving.set(false);
      }
    });
  }

  deleteDrone(drone: Drone): void {
    // Clear the previous result before the new one is shown (UI-004).
    this.clearMessages();
    this.deletingId.set(drone.id);

    this.droneService.delete(drone.id).subscribe({
      next: () => {
        this.drones.update((drones) => drones.filter((d) => d.id !== drone.id));
        this.deleteSuccessMessage.set(`Drone "${drone.name}" deleted.`);
        this.deletingId.set(null);
      },
      error: (error: unknown) => {
        // Only the backend decides whether a Drone can be deleted (design D1).
        this.deleteErrorMessage.set(problemDetailsMessage(error));
        this.deletingId.set(null);
      }
    });
  }

  private clearMessages(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.deleteErrorMessage.set(null);
    this.deleteSuccessMessage.set(null);
  }
}
