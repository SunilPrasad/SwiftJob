import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { EmailsApi } from '../../../core/api/emails-api';
import { JobDetail, JobStatus, WORK_MODE_LABELS } from '../../../core/models/job';
import { safeExternalUrl } from '../../../shared/external-url';
import { JobStatusChange } from '../shared/job-status-actions';
import { JobStatusChip } from '../shared/job-status-chip';

@Component({
  selector: 'app-job-details-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    MatButtonModule,
    MatDividerModule,
    MatExpansionModule,
    MatIconModule,
    MatProgressBarModule,
    MatTabsModule,
    MatTooltipModule,
    JobStatusChip,
  ],
  templateUrl: './job-details-panel.html',
  styleUrl: './job-details-panel.scss',
})
export class JobDetailsPanel {
  private readonly emailsApi = inject(EmailsApi);

  readonly job = input<JobDetail | undefined>();
  readonly loading = input(false);

  readonly closed = output<void>();
  readonly statusChange = output<JobStatusChange>();

  protected readonly workModeLabels = WORK_MODE_LABELS;
  protected readonly applicationUrl = computed(() => safeExternalUrl(this.job()?.applicationUrl));

  /** Which original email's text is expanded (loaded on demand). */
  protected readonly openEmailId = signal<number | null>(null);
  protected readonly openEmail = rxResource({
    params: () => this.openEmailId() ?? undefined,
    stream: ({ params }) => this.emailsApi.getEmail(params),
  });

  private readonly jobId = computed(() => this.job()?.id);

  constructor() {
    // Collapse any open email when a different job is shown (not when the same job is refreshed).
    effect(() => {
      this.jobId();
      untracked(() => this.openEmailId.set(null));
    });
  }

  protected setStatus(job: JobDetail, status: JobStatus): void {
    this.statusChange.emit({ jobId: job.id, status, previousStatus: job.status, title: job.title });
  }

  protected toggleEmail(emailId: number, expanded: boolean): void {
    if (expanded) {
      this.openEmailId.set(emailId);
    } else if (this.openEmailId() === emailId) {
      this.openEmailId.set(null);
    }
  }

  protected safeUrl(url: string): string | null {
    return safeExternalUrl(url);
  }
}
