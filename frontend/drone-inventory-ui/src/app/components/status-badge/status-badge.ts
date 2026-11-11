import { Component, computed, input } from '@angular/core';

/** Drone status values shown as a badge. */
export type DroneStatus = 'Available' | 'Loaned';

/** Loan status values shown as a badge. */
export type LoanStatus = 'Active' | 'Returned';

export type Status = DroneStatus | LoanStatus;

/**
 * Presentational badge for Drone and Loan status (design D2).
 *
 * Available and Returned use the success style, Loaned and Active the warning
 * style. The status text is always rendered, so the status is never conveyed by
 * color alone (UI-002).
 */
@Component({
  selector: 'app-status-badge',
  template: `
    <span class="badge" [class.badge--success]="tone() === 'success'" [class.badge--warning]="tone() === 'warning'">{{
      status()
    }}</span>
  `,
  styles: `
    .badge {
      display: inline-block;
      padding: 2px var(--space-2);
      border: 1px solid;
      border-radius: 999px;
      font-size: 0.8125rem;
      font-weight: 600;
      line-height: 1.4;
      white-space: nowrap;
    }

    .badge--success {
      border-color: var(--success);
      background: color-mix(in srgb, var(--success) 10%, var(--surface));
      color: var(--success);
    }

    .badge--warning {
      border-color: var(--warning);
      background: color-mix(in srgb, var(--warning) 10%, var(--surface));
      color: var(--warning);
    }
  `
})
export class StatusBadge {
  readonly status = input.required<Status>();

  protected readonly tone = computed<'success' | 'warning'>(() =>
    this.status() === 'Available' || this.status() === 'Returned' ? 'success' : 'warning'
  );
}
