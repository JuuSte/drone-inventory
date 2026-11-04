import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { problemDetailsMessage } from '../../core/problem-details';
import { Drone } from '../../models/drone.model';
import { DroneService } from '../../services/drone.service';

@Component({
  selector: 'app-drones-page',
  imports: [FormsModule],
  template: `
    <h1>Drones</h1>

    <form class="create-form" (ngSubmit)="create()">
      <label>
        Name
        <input name="name" [ngModel]="name()" (ngModelChange)="name.set($event)" />
      </label>
      <label>
        Description
        <textarea
          name="description"
          rows="2"
          [ngModel]="description()"
          (ngModelChange)="description.set($event)"
        ></textarea>
      </label>
      <button type="submit" [disabled]="saving()">Create drone</button>
    </form>

    @if (errorMessage(); as message) {
      <p class="error" role="alert">{{ message }}</p>
    }

    <table class="drone-list">
      <thead>
        <tr>
          <th>Name</th>
          <th>Status</th>
          <th>Description</th>
        </tr>
      </thead>
      <tbody>
        @for (drone of drones(); track drone.id) {
          <tr>
            <td>{{ drone.name }}</td>
            <td>
              <span class="status" [class.loaned]="drone.status === 'Loaned'">{{ drone.status }}</span>
            </td>
            <td>{{ drone.description }}</td>
          </tr>
        } @empty {
          <tr>
            <td colspan="3">No drones yet.</td>
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

    input,
    textarea {
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

    .status {
      color: #2f7d4f;
    }

    .status.loaned {
      color: #a12a2a;
    }
  `
})
export class DronesPage implements OnInit {
  private readonly droneService = inject(DroneService);

  readonly drones = signal<Drone[]>([]);
  readonly name = signal('');
  readonly description = signal('');
  readonly errorMessage = signal<string | null>(null);
  readonly saving = signal(false);

  ngOnInit(): void {
    this.droneService.getAll().subscribe({
      next: (drones) => this.drones.set(drones),
      error: (error: unknown) => this.errorMessage.set(problemDetailsMessage(error))
    });
  }

  create(): void {
    this.errorMessage.set(null);
    this.saving.set(true);

    this.droneService.create({ name: this.name(), description: this.description() }).subscribe({
      next: (drone) => {
        this.drones.update((drones) => [...drones, drone]);
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
}
