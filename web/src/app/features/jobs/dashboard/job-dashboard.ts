import { BreakpointObserver } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { map } from 'rxjs';
import { JOB_STATUS_LABELS, JobListItem, JobStatus } from '../../../core/models/job';
import { JobsStore } from '../data/jobs-store';
import { JobDetailsPanel } from '../details/job-details-panel';
import { JobFilters } from '../filters/job-filters';
import { JobStatusChange } from '../shared/job-status-actions';
import { JobTable } from '../table/job-table';

@Component({
  selector: 'app-job-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [JobsStore],
  imports: [
    MatButtonModule,
    MatIconModule,
    MatSidenavModule,
    MatTooltipModule,
    JobDetailsPanel,
    JobFilters,
    JobTable,
  ],
  templateUrl: './job-dashboard.html',
  styleUrl: './job-dashboard.scss',
})
export class JobDashboard {
  protected readonly store = inject(JobsStore);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly isHandset = toSignal(
    inject(BreakpointObserver)
      .observe('(max-width: 720px)')
      .pipe(map((s) => s.matches)),
    { initialValue: false },
  );

  protected readonly panelOpen = computed(() => this.store.selectedJobId() !== null);
  protected readonly page = computed(() => this.store.jobs.value());

  protected openJob(job: JobListItem): void {
    this.store.select(job.id);
  }

  protected closePanel(): void {
    this.store.select(null);
  }

  protected changeStatusFilter(status: JobStatus | null): void {
    this.store.setStatusFilter(status);
  }

  protected changeJobStatus(change: JobStatusChange): void {
    // Failures are already reported to the user by apiErrorInterceptor.
    this.store.updateStatus(change.jobId, change.status).subscribe({
      next: () => {
        const ref = this.snackBar.open(
          `“${change.title}” marked as ${JOB_STATUS_LABELS[change.status].toLowerCase()}`,
          'Undo',
          { duration: 5000 },
        );
        ref
          .onAction()
          .subscribe(() =>
            this.store
              .updateStatus(change.jobId, change.previousStatus)
              .subscribe({ error: () => undefined }),
          );
      },
      error: () => undefined,
    });
  }

  protected refresh(): void {
    this.store.refresh();
  }
}
