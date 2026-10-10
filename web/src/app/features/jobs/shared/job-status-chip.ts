import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { JOB_STATUS_LABELS, JobStatus } from '../../../core/models/job';

@Component({
  selector: 'app-job-status-chip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="chip" [class]="'chip status-' + status()">{{ label() }}</span>`,
  styles: `
    .chip {
      display: inline-block;
      padding: 2px 10px;
      border-radius: 12px;
      font: var(--mat-sys-label-medium);
      white-space: nowrap;
    }
    .status-New {
      background: var(--mat-sys-primary-container);
      color: var(--mat-sys-on-primary-container);
    }
    .status-Applied {
      background: #d7f5dd;
      color: #0d5c25;
    }
    .status-NotRelevant {
      background: var(--mat-sys-surface-container-highest);
      color: var(--mat-sys-on-surface-variant);
    }
  `,
})
export class JobStatusChip {
  readonly status = input.required<JobStatus>();
  protected readonly label = computed(() => JOB_STATUS_LABELS[this.status()]);
}
